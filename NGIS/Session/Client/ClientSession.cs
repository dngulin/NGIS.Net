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
    private readonly IGameClient _gameClient;
    private readonly ILogger _log;

    private readonly ClientSideMsgPipe _pipe;
    private readonly byte[] _sendBuffer;

    public ClientSession(ClientConfig config, IGameClient gameClient, ILogger log) {
      _gameClient = gameClient;
      _log = log;
      _sendBuffer = new byte[MsgConstants.MaxClientMsgSize];

      _log?.Info($"Connecting to {config.Host}:{config.Port}...");
      var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      socket.Connect(config.Host, config.Port);

      _log?.Info($"Joining as '{config.PlayerName}' [game: {config.Game}, version: {config.Version}]...");
      _pipe = new ClientSideMsgPipe(socket, config.MaxPlayers * MsgConstants.MaxServerMsgPartSize);
      _pipe.SendMessageUsingBuffer(new ClientMsgJoin(config.Game, config.Version, config.PlayerName), _sendBuffer);

      State = ClientSessionState.Joining;
    }

    public ClientSessionState State { get; private set; }

    public void Process() {
      if (State == ClientSessionState.Closed)
        return;

      Exception exception = null;
      Action handler = null;

      try {
        ProcessState();
      }
      catch (ServerErrorException e) {
        exception = e;
        handler = () => _gameClient.SessionClosedByServerError(e.Error);
      }
      catch (SocketException e) {
        exception = e;
        handler = () => _gameClient.SessionClosedByConnectionError();
      }
      catch (ProtocolException e) {
        exception = e;
        handler = () => _gameClient.SessionClosedByProtocolError();
      }
      catch (Exception e) {
        exception = e;
        handler = () => _gameClient.SessionClosedByInternalError();
      }

      if (exception == null) return;

      _log?.Error("An exception thrown during session processing!");
      _log?.Exception(exception);

      CloseSession();
      handler();
    }

    private void ProcessState() {
      _pipe.ReceiveMessages();

      if (!_pipe.IsConnected() || _pipe.IsReceiveTimeout()) {
        _log?.Error("Connection lost!");
        CloseSession();
        _gameClient.SessionClosedByConnectionError();
        return;
      }

      switch (State) {
        case ClientSessionState.Joining:
          var joined = ProcessJoiningStateMessages();
          if (joined) {
            State = ClientSessionState.WaitingPlayers;
            _log.Info("Joined! Waining for players...");
          }
          break;

        case ClientSessionState.WaitingPlayers:
          TrySendKeepAlive();
          var optMsgStart = ProcessWaitingStateMessages();
          if (optMsgStart.HasValue) {
            State = ClientSessionState.Active;
            _log.Info("Game started!");
            _gameClient.SessionStarted(optMsgStart.Value);
          }
          break;

        case ClientSessionState.Active:
          var optMsgFinish = ProcessActiveStateMessages();
          if (optMsgFinish.HasValue) {
            CloseSession();
            _log.Info("Game finished!");
            _gameClient.SessionFinished(optMsgFinish.Value);
            return;
          }

          var (clientInputs, clientFinish) = _gameClient.Process();
          SendMessages(clientInputs, clientFinish);
          TrySendKeepAlive();
          break;

        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private void TrySendKeepAlive() {
      if (_pipe.IsKeepAliveTimeout())
        _pipe.SendMessageUsingBuffer(new ClientMsgKeepAlive(), _sendBuffer);
    }

    private bool ProcessJoiningStateMessages() {
      var joined = false;

      while (_pipe.ReceiveOrder.Count > 0) {
        var msgId = _pipe.ReceiveOrder.Dequeue();
        switch (msgId) {
          case ServerMsgId.Joined:
            if (joined) throw new ProtocolException("Join message received twice");
            joined = true;
            break;

          case ServerMsgId.Error:
            throw new ServerErrorException(_pipe.ErrorMessages.Dequeue().ErrorId);
          default:
            throw new ProtocolException($"Wrong message ({msgId}) is received during join state");
        }
      }

      return joined;
    }

    private ServerMsgStart? ProcessWaitingStateMessages() {
      ServerMsgStart? msgStart = null;

      while (_pipe.ReceiveOrder.Count > 0) {
        var msgId = _pipe.ReceiveOrder.Dequeue();
        switch (msgId) {
          case ServerMsgId.KeepAlive:
            break;

          case ServerMsgId.Start:
            if (msgStart.HasValue) throw new ProtocolException("Start message received twice");
            msgStart = _pipe.StartMessages.Dequeue();
            break;

          case ServerMsgId.Error:
            throw new ServerErrorException(_pipe.ErrorMessages.Dequeue().ErrorId);
          default:
            throw new ProtocolException($"Wrong message ({msgId}) is received during wait state");
        }
      }

      return msgStart;
    }

    private ServerMsgFinish? ProcessActiveStateMessages() {
      ServerMsgFinish? msgFinish = null;

      while (_pipe.ReceiveOrder.Count > 0) {
        var msgId = _pipe.ReceiveOrder.Dequeue();
        switch (msgId) {
          case ServerMsgId.KeepAlive:
            break;

          case ServerMsgId.Inputs:
            _gameClient.InputReceived(_pipe.InputMessages.Dequeue());
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

    private void SendMessages(Queue<ClientMsgInputs> inputs, ClientMsgFinished? result) {
      while (inputs.Count > 0)
        _pipe.SendMessageUsingBuffer(inputs.Dequeue(), _sendBuffer);

      if (result.HasValue)
        _pipe.SendMessageUsingBuffer(result.Value, _sendBuffer);
    }

    public void Dispose() {
      if (State == ClientSessionState.Closed)
        return;

      CloseSession();
    }

    private void CloseSession() {
      _pipe.Close();
      State = ClientSessionState.Closed;
      _log?.Info("Session closed");
    }
  }
}