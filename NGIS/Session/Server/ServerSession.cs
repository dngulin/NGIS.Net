using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using NGIS.Logging;
using NGIS.Message;
using NGIS.Message.Client;
using NGIS.Message.Server;
using NGIS.Pipe.Server;

namespace NGIS.Session.Server {
  public class ServerSession : IDisposable {
    private readonly byte _playersCount;
    private readonly byte _tps;

    private readonly ILogger _log;
    private readonly string _id;

    private readonly List<(ServerSideMsgPipe Pipe, string NickName)> _clients;
    private readonly byte[] _sendBuffer;

    public SessionState State { get; private set; }

    public bool NeedClient => State == SessionState.Preparing && _clients.Count < _playersCount;

    public bool HasClientWithName(string nickName) {
      foreach (var (_, name) in _clients) {
        if (name == nickName)
          return true;
      }

      return false;
    }

    public ServerSession(int id, byte playersCount, byte tps, int sendBufferSize, ILogger log) {
      State = SessionState.Preparing;

      _playersCount = playersCount;
      _tps = tps;

      _log = log;
      _id = id.ToString();

      _clients = new List<(ServerSideMsgPipe, string)>(playersCount);
      _sendBuffer = new byte[sendBufferSize];

      _log?.Info($"Created session {_id}");
    }

    public void AddClient(ServerSideMsgPipe pipe, string nickName) {
      if (!NeedClient || HasClientWithName(nickName))
        throw new InvalidOperationException();

      _clients.Add((pipe, nickName));
      _log?.Info($"Client {pipe.Id} '{nickName}' joined to session {_id}");
    }

    public void Process() {
      if (State == SessionState.Closed)
        return;

      ServerErrorId? error = null;
      Exception exception = null;
      try {
        ProcessState();
      }
      catch (ProtocolException e) {
        exception = e;
        error = ServerErrorId.ProtocolError;
      }
      catch (SocketException e) {
        exception = e;
        error = ServerErrorId.ConnectionError;
      }
      catch (Exception e) {
        exception = e;
        error = ServerErrorId.InternalError;
      }

      if (error == null) return;

      SafeSendMsgToAllClients(new ServerMsgError(error.Value));
      CloseConnections();
      State = SessionState.Closed;

      _log?.Error($"Session {_id} closed with error id {error.Value}");
      _log?.Exception(exception);
    }

    private void ProcessState() {
      RemoveDisconnectedClients();
      ReceiveAll();

      switch (State) {
        case SessionState.Preparing:
          CheckPreparingClients();
          if (_clients.Count < _playersCount) {
            SendKeepAliveIfNeed();
            return;
          }
          SendStart();
          State = SessionState.Active;
          _log?.Info($"Session {_id} started");
          break;

        case SessionState.Active:
          if (_clients.Count < _playersCount) {
            SafeSendMsgToAllClients(new ServerMsgError(ServerErrorId.ConnectionError));
            CloseConnections();
            State = SessionState.Closed;
            _log?.Error($"Session {_id} closed with connection error");
            break;
          }

          SendReceivedInputs();
          SendKeepAliveIfNeed();

          if (_clients.All(c => c.Pipe.FinishedMessages.Count > 0)) {
            SendFinish();
            CloseConnections();
            State = SessionState.Closed;
            _log?.Info($"Session {_id} finished and closed");
          }
          break;
      }
    }

    private void RemoveDisconnectedClients() {
      foreach (var (pipe, nickName) in _clients) {
        if (!pipe.IsConnected() || pipe.IsReceiveTimeout()) {
          pipe.Close();
          _log?.Warning($"Remove disconnected client {pipe.Id} '{nickName}' from session {_id}");
        }
      }

      _clients.RemoveAll(c => c.Pipe.Closed);
    }

    private void CheckPreparingClients() {
      foreach (var (pipe, nickName) in _clients) {
        while (pipe.ReceiveOrder.Count > 0) {
          if (pipe.ReceiveOrder.Dequeue() == ClientMsgId.KeepAlive)
            continue;

          try {
            pipe.SendMessageUsingBuffer(new ServerMsgError(ServerErrorId.ProtocolError), _sendBuffer);
          }
          finally {
            pipe.Close();
            _log?.Warning($"Remove client {pipe.Id} '{nickName}' from session {_id} because of protocol error");
          }
        }
      }

      _clients.RemoveAll(c => c.Pipe.Closed);
    }


    private void SendKeepAliveIfNeed() {
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
      _log?.Info($"Sending finish message for session {_id}...");
      var frames = new uint[_playersCount];
      var hashes = new int[_playersCount];

      for (byte clientIndex = 0; clientIndex < _playersCount; clientIndex++) {
        var (pipe, nickName) = _clients[clientIndex];

        var msg = pipe.FinishedMessages.Dequeue();
        frames[clientIndex] = msg.Frame;
        hashes[clientIndex] = msg.StateHash;

        _log?.Info($"Client {pipe.Id} '{nickName}' finished at {msg.Frame} with state hash {msg.StateHash}");
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
      _log?.Warning($"Session {_id} closed externally");
    }
  }
}