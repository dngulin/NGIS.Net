using System;
using System.Collections.Generic;
using System.Net.Sockets;
using NGIS.Message.Client;
using NGIS.Message.Server;
using NGIS.Pipe.Client;

namespace NGIS.Session.Client {
  public class ClientSession : IDisposable {
    private readonly IGameClient _gameClient;

    private readonly ClientSideMsgPipe _pipe;
    private readonly byte[] _sendBuffer;

    public ClientSession(ClientConfig config, IGameClient gameClient) {
      _gameClient = gameClient;
      _sendBuffer = new byte[528];

      var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      socket.Connect(config.Host, config.Port);

      _pipe = new ClientSideMsgPipe(socket, 272 * config.MaxPlayers);
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
      catch (Exception e) {
        exception = e;
        handler = () => _gameClient.SessionClosedByInternalError();
      }

      if (exception == null) return;

      CloseSession();
      handler.Invoke();
    }

    private void ProcessState() {
      _pipe.ReceiveMessages();

      if (!_pipe.IsConnected() || _pipe.IsReceiveTimeout()) {
        CloseSession();
        _gameClient.SessionClosedByConnectionError();
        return;
      }

      switch (State) {
        case ClientSessionState.Joining:
          var joined = ProcessJoiningMessages();
          if (joined) State = ClientSessionState.WaitingPlayers;
          break;

        case ClientSessionState.WaitingPlayers:
          TrySendKeepAlive();
          var optMsgStart = ProcessWaitingMessages();
          if (optMsgStart.HasValue) {
            State = ClientSessionState.Active;
            _gameClient.SessionStarted(optMsgStart.Value);
          }
          break;

        case ClientSessionState.Active:
          var optMsgFinish = ProcessActiveMessages();
          if (optMsgFinish.HasValue) {
            CloseSession();
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

    private bool ProcessJoiningMessages() {
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

    private ServerMsgStart? ProcessWaitingMessages() {
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

    private ServerMsgFinish? ProcessActiveMessages() {
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
    }
  }
}