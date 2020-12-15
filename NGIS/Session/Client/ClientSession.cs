using System;
using System.Collections.Generic;
using System.Net.Sockets;
using NGIS.Logging;
using NGIS.Message;
using NGIS.Message.Client;
using NGIS.Message.Server;
using NGIS.Pipe.Client;

namespace NGIS.Session.Client {
  public class ClientSession : IDisposable {
    private readonly ILogger _log;

    private readonly byte[] _sendBuffer;
    private readonly ClientSideMsgPipe _pipe;

    private readonly Queue<ServerMsgInput> _receivedInputs = new Queue<ServerMsgInput>(16);

    public ClientSession(ClientConfig config, ILogger log) {
      _log = log;
      _sendBuffer = new byte[MsgConstants.MaxClientMsgSize];

      _log?.Info($"Connecting to {config.Host}:{config.Port}...");
      var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) {NoDelay = true};
      socket.Connect(config.Host, config.Port);

      _log?.Info($"Joining as '{config.PlayerName}' [game: {config.Game}, version: {config.Version}]...");
      _pipe = new ClientSideMsgPipe(socket, config.MaxPlayers * MsgConstants.MaxServerMsgPartSize);
      State = ClientSessionState.Joining;
      _pipe.SendMessageUsingBuffer(new ClientMsgJoin(config.Game, config.Version, config.PlayerName), _sendBuffer);
    }

    public ClientSessionState State { get; private set; }

    public Queue<ServerMsgInput> ReceivedInputs => _receivedInputs;

    public ProcessingResult Process() {
      if (State == ClientSessionState.Closed)
        return ProcessingResult.None();

      ProcessingResult result;

      try {
        return ProcessState();
      }
      catch (ServerErrorException e) {
        result = ProcessingResult.Error(SessionError.ServerError, e.Error);
        _log?.Exception(e);
      }
      catch (SocketException e) {
        result = ProcessingResult.Error(SessionError.ConnectionError);
        _log?.Exception(e);
      }
      catch (ProtocolException e) {
        result = ProcessingResult.Error(SessionError.ProtocolError);
        _log?.Exception(e);
      }
      catch (Exception e) {
        result = ProcessingResult.Error(SessionError.InternalError);
        _log?.Exception(e);
      }

      CloseSession();
      return result;
    }

    private ProcessingResult ProcessState() {
      _pipe.ReceiveMessages();

      if (!_pipe.IsConnected || _pipe.IsReceiveTimeout()) {
        _log?.Error("Connection lost!");
        CloseSession();
        return ProcessingResult.Error(SessionError.ConnectionError);
      }

      switch (State) {
        case ClientSessionState.Joining:
          var joined = ProcessJoiningStateMessages();
          if (joined) {
            State = ClientSessionState.WaitingPlayers;
            _log?.Info("Joined! Waining for players...");
            return ProcessingResult.Joined();
          }
          break;

        case ClientSessionState.WaitingPlayers:
          TrySendKeepAlive();
          var optMsgStart = ProcessWaitingStateMessages();
          if (optMsgStart.HasValue) {
            State = ClientSessionState.Active;
            _log?.Info("Game started!");
            return ProcessingResult.Started(optMsgStart.Value);
          }
          break;

        case ClientSessionState.Active:
          var optMsgFinish = ProcessActiveStateMessages();
          if (optMsgFinish.HasValue) {
            CloseSession();
            _log?.Info("Game finished!");
            return ProcessingResult.Finished(optMsgFinish.Value);
          }

          TrySendKeepAlive();
          return ProcessingResult.Active();

        default:
          throw new ArgumentOutOfRangeException();
      }

      return ProcessingResult.None();
    }

    private void TrySendKeepAlive() {
      if (_pipe.IsKeepAliveTimeout())
        _pipe.SendMessageUsingBuffer(new ClientMsgKeepAlive(), _sendBuffer);
    }

    private bool ProcessJoiningStateMessages() {
      while (_pipe.ReceiveOrder.Count > 0) {
        var msgId = _pipe.ReceiveOrder.Dequeue();
        switch (msgId) {
          case ServerMsgId.Joined:
            return true;

          case ServerMsgId.Error:
            throw new ServerErrorException(_pipe.ErrorMessages.Dequeue().ErrorId);

          default:
            throw new ProtocolException($"Wrong message ({msgId}) is received during join state");
        }
      }

      return false;
    }

    private ServerMsgStart? ProcessWaitingStateMessages() {
      while (_pipe.ReceiveOrder.Count > 0) {
        var msgId = _pipe.ReceiveOrder.Dequeue();
        switch (msgId) {
          case ServerMsgId.KeepAlive:
            break;

          case ServerMsgId.Start:
            return _pipe.StartMessages.Dequeue();

          case ServerMsgId.Error:
            throw new ServerErrorException(_pipe.ErrorMessages.Dequeue().ErrorId);
          default:
            throw new ProtocolException($"Wrong message ({msgId}) is received during wait state");
        }
      }

      return null;
    }

    private ServerMsgFinish? ProcessActiveStateMessages() {
      ServerMsgFinish? msgFinish = null;

      while (_pipe.ReceiveOrder.Count > 0) {
        var msgId = _pipe.ReceiveOrder.Dequeue();
        switch (msgId) {
          case ServerMsgId.KeepAlive:
            break;

          case ServerMsgId.Inputs:
            _receivedInputs.Enqueue(_pipe.InputMessages.Dequeue());
            break;

          case ServerMsgId.Finish:
            if (msgFinish.HasValue) throw new ProtocolException("Finish message received twice");
            msgFinish = _pipe.FinishMessages.Dequeue();
            break;

          case ServerMsgId.Error:
            throw new ServerErrorException(_pipe.ErrorMessages.Dequeue().ErrorId);
          default:
            throw new ProtocolException($"Wrong message ({msgId}) is received during active state");
        }
      }

      return msgFinish;
    }

    public SessionError? SendMessages(Queue<ClientMsgInputs> inputs, ClientMsgFinished? result) {
      try {
        while (inputs.Count > 0)
          _pipe.SendMessageUsingBuffer(inputs.Dequeue(), _sendBuffer);

        if (result.HasValue)
          _pipe.SendMessageUsingBuffer(result.Value, _sendBuffer);
      }
      catch (SocketException e) {
        _log?.Exception(e);
        return SessionError.ConnectionError;
      }
      catch (Exception e) {
        _log?.Exception(e);
        return SessionError.InternalError;
      }

      return null;
    }

    public void Dispose() => CloseSession();

    private void CloseSession() {
      if (State == ClientSessionState.Closed)
        return;

      _pipe.Close();
      State = ClientSessionState.Closed;
      _log?.Info("Session closed");
    }
  }
}