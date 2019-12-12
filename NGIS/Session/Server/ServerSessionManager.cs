using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NGIS.Message.Client;
using NGIS.Message.Server;
using NGIS.Pipe.Server;

namespace NGIS.Session.Server {
  public class ServerSessionManager: IDisposable {
    private readonly string _game;
    private readonly uint _version;

    private readonly int _maxSessions;
    private readonly byte _sessionPlayers;
    private readonly byte _tps;

    private readonly Socket _serverSocket;
    private readonly byte[] _sendBuffer = new byte[32];

    private readonly List<ServerSideMsgPipe> _joiningPool;
    private readonly Stack<int> _toRemoveFromPool;

    private readonly List<ServerSession> _sessions;

    private bool _disposed;

    public ServerSessionManager(ServerConfig config) {
      _game = config.Game;
      _version = config.Version;

      _maxSessions = config.MaxSessions;
      _sessionPlayers = config.SessionPlayers;
      _tps = config.TickPerSecond;

      var ip = Dns.GetHostAddresses(config.Host).First();
      var ep = new IPEndPoint(ip, config.Port);

      _sessions = new List<ServerSession>(config.MaxSessions);
      _joiningPool = new List<ServerSideMsgPipe>(config.SessionPlayers);
      _toRemoveFromPool = new Stack<int>(config.SessionPlayers);

      _serverSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp) {NoDelay = true};
      _serverSocket.Bind(ep);
      _serverSocket.Listen(config.SessionPlayers);
    }

    public void Porcess() {
      if (_disposed)
        throw new ObjectDisposedException(nameof(ServerSessionManager));

      AcceptNewClients();
      CleanupJoiningPool();

      ReceiveAll();
      ProcessJoiningPool();

      _sessions.ForEach(s => s.Process());
      _sessions.RemoveAll(s => s.State == SessionState.Closed);
    }

    private void AcceptNewClients() {
      if (_serverSocket.Poll(1000, SelectMode.SelectRead)) {
        _joiningPool.Add(new ServerSideMsgPipe(_serverSocket.Accept(), 528));
      }
    }

    private void CleanupJoiningPool() {
      foreach (var pipe in _joiningPool) {
        if (!pipe.IsConnected() || pipe.IsReceiveTimeout())
          pipe.Close();
      }

      _joiningPool.RemoveAll(p => p.Closed);
    }

    private void ReceiveAll() {
      foreach (var pipe in _joiningPool) {
        try {
          pipe.ReceiveMessages();
        }
        catch {
          ClosePipeWithError(pipe, ServerErrorId.InternalError);
        }
      }

      _joiningPool.RemoveAll(p => p.Closed);
    }

    private void ProcessJoiningPool() {
      for (var i = 0; i < _joiningPool.Count; i++) {
        var pipe = _joiningPool[i];
        if (pipe.ReceiveOrder.Count == 0)
          continue;

        _toRemoveFromPool.Push(i);

        var msgId = pipe.ReceiveOrder.Dequeue();
        if (msgId != ClientMsgId.Join || pipe.ReceiveOrder.Count > 0) {
          ClosePipeWithError(pipe, ServerErrorId.ProtocolError);
          continue;
        }

        var joinMsg = pipe.JoinMessages.Dequeue();
        if (joinMsg.ProtocolVersion != _version || joinMsg.GameName != _game) {
          ClosePipeWithError(pipe, ServerErrorId.Incompatible);
          continue;
        }

        TryJoinToSession(pipe, joinMsg.PlayerName);
      }

      while (_toRemoveFromPool.Count > 0)
        _joiningPool.RemoveAt(_toRemoveFromPool.Pop());
    }

    private void TryJoinToSession(ServerSideMsgPipe pipe, string nickName) {
      var joiningSession = _sessions.FirstOrDefault(s => s.NeedClient);

      switch (joiningSession) {
        case null when _sessions.Count >= _maxSessions:
          ClosePipeWithError(pipe, ServerErrorId.ServerIsBusy);
          return;

        case null:
          joiningSession = new ServerSession(_sessionPlayers, _tps, 272 * _sessionPlayers);
          _sessions.Add(joiningSession);
          break;

        case ServerSession session when session.HasClientWithName(nickName):
          ClosePipeWithError(pipe, ServerErrorId.NickIsBusy);
          return;
      }

      pipe.SendMessageUsingBuffer(new ServerMsgJoined(), _sendBuffer);
      joiningSession.AddClient(pipe, nickName);
    }

    private void ClosePipeWithError(ServerSideMsgPipe pipe, ServerErrorId error) {
      try {
        pipe.SendMessageUsingBuffer(new ServerMsgError(error), _sendBuffer);
      }
      finally {
        pipe.Close();
      }
    }

    public void Dispose() {
      if (_disposed)
        return;

      _serverSocket?.Dispose();
      _joiningPool.ForEach(p => p.Close());
      _sessions.ForEach(s => s.Dispose());

      _disposed = true;
    }
  }
}