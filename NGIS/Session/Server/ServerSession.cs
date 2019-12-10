using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using NGIS.Message;
using NGIS.Message.Client;
using NGIS.Message.Server;
using NGIS.Pipe.Server;

namespace NGIS.Session.Server {
  public class ServerSession : IDisposable {
    private readonly byte _playersCount;
    private readonly byte _tps;

    private readonly List<(ServerSideMsgPipe Pipe, string NickName)> _clients;
    private readonly byte[] _sendBuffer;

    public SessionState State { get; private set; }

    public bool NeedClient => State == SessionState.Preparing && _clients.Count < _playersCount;

    public ServerSession(byte playersCount, byte tps) {
      State = SessionState.Preparing;

      _playersCount = playersCount;
      _tps = tps;

      _clients = new List<(ServerSideMsgPipe, string)>(playersCount);
      _sendBuffer = new byte[1024];
    }

    public void AddClient(ServerSideMsgPipe pipe, string nickName) {
      if (!NeedClient)
        throw new InvalidOperationException();

      _clients.Add((pipe, nickName));
    }

    public void Process() {
      if (State == SessionState.Closed)
        return;

      ServerErrorId? error = null;
      try {
        ProcessState();
      }
      catch (ProtocolException) {
        error = ServerErrorId.ProtocolError;
      }
      catch (SocketException) {
        error = ServerErrorId.ConnectionError;
      }
      catch (Exception) {
        error = ServerErrorId.InternalError;
      }
      finally {
        if (error != null) {
          SafeSendMsgToAllClients(new ServerMsgError(error.Value));
          CloseConnections();
          State = SessionState.Closed;
        }
      }
    }

    private void ProcessState() {
      RemoveDisconnectedClients();
      ReceiveAll();

      switch (State) {
        case SessionState.Preparing:
          if (_clients.Count < _playersCount) {
            SendKeepAliveIsNeed();
            return;
          }
          SendStart();
          State = SessionState.Active;
          break;

        case SessionState.Active:
          if (_clients.Count < _playersCount) {
            SafeSendMsgToAllClients(new ServerMsgError(ServerErrorId.ConnectionError));
            CloseConnections();
            State = SessionState.Closed;
            break;
          }

          SendReceivedInputs();
          SendKeepAliveIsNeed();

          if (_clients.All(c => c.Pipe.FinishedMessages.Count > 0)) {
            SendFinish();
            CloseConnections();
            State = SessionState.Closed;
          }
          break;
      }
    }

    private void RemoveDisconnectedClients() {
      foreach (var (pipe, _) in _clients) {
        if (!pipe.IsConnected() || pipe.IsReceiveTimeout())
          pipe.Close();
      }

      _clients.RemoveAll(c => c.Pipe.Closed);
    }

    private void SendKeepAliveIsNeed() {
      var keepAlive = new ServerMsgKeepAlive();

      foreach (var (pipe, _) in _clients) {
        if (pipe.IsKeepAliveTimeout())
          pipe.SendMessageUsingBuffer(keepAlive, _sendBuffer);
      }
    }

    private void SendStart() {
      var seed = new Random().Next();
      var players = _clients.Select(c => c.NickName).ToArray();

      for (byte clientIndex = 0; clientIndex < _playersCount; clientIndex++) {
        var startMessage = new ServerMsgStart(seed, players, clientIndex, _tps);
        _clients[clientIndex].Pipe.SendMessageUsingBuffer(startMessage, _sendBuffer);
      }
    }

    private void SendFinish() {
      var frames = new uint[_playersCount];
      var hashes = new int[_playersCount];

      for (byte clientIndex = 0; clientIndex < _playersCount; clientIndex++) {
        var msg = _clients[clientIndex].Pipe.FinishedMessages.Dequeue();
        frames[clientIndex] = msg.Frame;
        hashes[clientIndex] = msg.StateHash;
      }

      var finishMsg = new ServerMsgFinish(frames, hashes);
      foreach (var client in _clients)
        client.Pipe.SendMessageUsingBuffer(finishMsg, _sendBuffer);
    }

    private void CloseConnections() {
      _clients.ForEach(c => c.Pipe.Close());
      _clients.Clear();
    }

    private void ReceiveAll() => _clients.ForEach(c => c.Pipe.ReceiveMessages());

    private void SendReceivedInputs() {
      for (byte clientIndex = 0; clientIndex < _playersCount; clientIndex++) {
        var (pipe, nick) = _clients[clientIndex];

        while (pipe.ReceiveOrder.Count > 0) {
          var msgId = pipe.ReceiveOrder.Dequeue();
          switch (msgId) {
            case ClientMsgId.KeepAlive:
              break;

            case ClientMsgId.Inputs:
              SendMsgToAllClientsExcept(pipe.InputMessages.Dequeue().ToServerMsg(clientIndex), clientIndex);
              break;

            case ClientMsgId.Finished:
              if (pipe.FinishedMessages.Count > 1)
                throw new ProtocolException($"Multiple finish messages received from {nick}!");
              break;

            default:
              throw new ProtocolException($"Wrong message ({msgId}) received from {nick}!");
          }
        }
      }
    }

    private void SafeSendMsgToAllClients<T>(T msg) where T : struct, IServerSerializableMsg {
      foreach (var client in _clients)
        try {
          client.Pipe.SendMessageUsingBuffer(msg, _sendBuffer);
        }
        catch {
          // ignored
        }
    }

    private void SendMsgToAllClientsExcept<T>(T msg, byte exceptedIndex) where T : struct, IServerSerializableMsg {
      for (byte clientIndex = 0; clientIndex < _playersCount; clientIndex++) {
        if (clientIndex != exceptedIndex)
          _clients[clientIndex].Pipe.SendMessageUsingBuffer(msg, _sendBuffer);
      }
    }

    public void Dispose() {
      if (State == SessionState.Closed)
        return;

      SafeSendMsgToAllClients(new ServerMsgError(ServerErrorId.ConnectionError));
      CloseConnections();
      State = SessionState.Closed;
    }
  }
}