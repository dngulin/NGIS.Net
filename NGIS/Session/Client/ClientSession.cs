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
    private readonly IClientSessionWorker _worker;
    private readonly ILogger _log;

    private readonly ClientSideMsgPipe _pipe;
    private readonly byte[] _sendBuffer;

    public ClientSession(ClientConfig config, IClientSessionWorker worker, ILogger log) {
      _worker = worker;
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

      (Exception CatchedException, ClientSessionError Id, ServerErrorId? ServerErrorId) error = default;

      try {
        ProcessState();
      }
      catch (ServerErrorException exception) {
        error = (exception, ClientSessionError.ServerError, exception.Error);
      }
      catch (SocketException exception) {
        error = (exception, ClientSessionError.ConnectionError, null);
      }
      catch (ProtocolException exception) {
        error = (exception, ClientSessionError.ProtocolError, null);
      }
      catch (Exception exception) {
        error = (exception, ClientSessionError.InternalError, null);
      }

      if (error.CatchedException == null) return;

      _log?.Error("An exception thrown during session processing!");
      _log?.Exception(error.CatchedException);

      CloseSession();
      _worker.SessionClosedWithError(error.Id, error.ServerErrorId);
    }

    private void ProcessState() {
      _pipe.ReceiveMessages();

      if (!_pipe.IsConnected || _pipe.IsReceiveTimeout()) {
        _log?.Error("Connection lost!");
        CloseSession();
        _worker.SessionClosedWithError(ClientSessionError.ConnectionError);
        return;
      }

      switch (State) {
        case ClientSessionState.Joining:
          var joined = ProcessJoiningStateMessages();
          if (joined) {
            State = ClientSessionState.WaitingPlayers;
            _log?.Info("Joined! Waining for players...");
            _worker.JoinedToSession();
          }
          break;

        case ClientSessionState.WaitingPlayers:
          TrySendKeepAlive();
          var optMsgStart = ProcessWaitingStateMessages();
          if (optMsgStart.HasValue) {
            State = ClientSessionState.Active;
            _log?.Info("Game started!");
            _worker.SessionStarted(optMsgStart.Value);
          }
          break;

        case ClientSessionState.Active:
          var optMsgFinish = ProcessActiveStateMessages();
          if (optMsgFinish.HasValue) {
            CloseSession();
            _log?.Info("Game finished!");
            _worker.SessionFinished(optMsgFinish.Value);
            return;
          }

          var (clientInputs, clientFinish) = _worker.Process();
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
            _worker.InputReceived(_pipe.InputMessages.Dequeue());
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