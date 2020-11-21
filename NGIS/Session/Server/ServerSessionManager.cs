using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NGIS.Logging;
using NGIS.Message;
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

    private readonly ILogger _log;

    private readonly Socket _serverSocket;
    private readonly byte[] _sendBuffer = new byte[4];

    private readonly List<ServerSideMsgPipe> _joiningPool;
    private readonly Stack<int> _toRemoveFromPool;

    private int _lastSessionId;
    private readonly List<ServerSession> _sessions;

    private bool _disposed;

    public ServerSessionManager(ServerConfig config, ILogger log) {
      _game = config.Game;
      _version = config.Version;

      _maxSessions = config.MaxSessions;
      _sessionPlayers = config.SessionPlayers;
      _tps = config.TickPerSecond;

      _log = log;

      var ip = Dns.GetHostAddresses(config.Host).First(a => a.AddressFamily == AddressFamily.InterNetwork);
      var ep = new IPEndPoint(ip, config.Port);

      _sessions = new List<ServerSession>(config.MaxSessions);
      _joiningPool = new List<ServerSideMsgPipe>(config.SessionPlayers);
      _toRemoveFromPool = new Stack<int>(config.SessionPlayers);

      _serverSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp) {NoDelay = true};
      _serverSocket.Bind(ep);
      _serverSocket.Listen(config.SessionPlayers);
      _log?.Info($"Listening at {ep}...");
    }

    public void Process() {
      if (_disposed)
        throw new ObjectDisposedException(nameof(ServerSessionManager));

      AcceptNewClients();
      CleanupJoiningPool();

      ReceiveAll();
      ProcessJoiningPool();

      _sessions.ForEach(s => s.Process());
      _sessions.RemoveAll(s => s.State == ServerSessionState.Closed);
    }

    private void AcceptNewClients() {
      if (!_serverSocket.Poll(1000, SelectMode.SelectRead))
        return;

      var pipe = new ServerSideMsgPipe(_serverSocket.Accept(), MsgConstants.MaxClientMsgSize);
      _joiningPool.Add(pipe);

      _log?.Info($"Add client {pipe.Id} to join pool");
    }

    private void CleanupJoiningPool() {
      foreach (var pipe in _joiningPool) {
        if (!pipe.IsConnected || pipe.IsReceiveTimeout()) {
          pipe.Close();
          _log?.Warning($"Remove disconnected client {pipe.Id} from join pool");
        }
      }

      _joiningPool.RemoveAll(p => p.Closed);
    }

    private void ReceiveAll() {
      foreach (var pipe in _joiningPool) {
        try {
          pipe.ReceiveMessages();
        }
        catch (Exception e) {
          ClosePipeWithError(pipe, ServerErrorId.InternalError);
          _log?.Error($"Failed to receive messages from client {pipe.Id}. Connection closed.");
          _log?.Exception(e);
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
          _log?.Error($"Client {pipe.Id} doesn't respect the protocol ({msgId}:{pipe.ReceiveOrder.Count})");
          continue;
        }

        var joinMsg = pipe.JoinMessages.Dequeue();
        if (joinMsg.Version != _version || joinMsg.Game != _game) {
          ClosePipeWithError(pipe, ServerErrorId.Incompatible);
          _log?.Error($"Client {pipe.Id} is incompatible ({joinMsg.Game}:{joinMsg.Version})");
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
          _log?.Error($"Failed to add client {pipe.Id} into session: server is busy");
          return;

        case null:
          var sendBufferSize = _sessionPlayers * MsgConstants.MaxServerMsgPartSize;
          joiningSession = new ServerSession(_lastSessionId++, _sessionPlayers, _tps, sendBufferSize, _log);
          _sessions.Add(joiningSession);
          break;

        case {} session when session.HasClientWithName(nickName):
          ClosePipeWithError(pipe, ServerErrorId.NickIsBusy);
          _log?.Error($"Failed to add client {pipe.Id} into session: nickname is busy");
          return;
      }

      try {
        pipe.SendMessageUsingBuffer(new ServerMsgJoined(), _sendBuffer);
        joiningSession.AddClient(pipe, nickName);
      }
      catch (Exception e) {
        ClosePipeWithError(pipe, ServerErrorId.InternalError);
        _log?.Error($"Failed to add client {pipe.Id} into session: internal error");
        _log?.Exception(e);
      }
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

      _log?.Info("Closing all connections...");

      _serverSocket?.Dispose();
      _joiningPool.ForEach(p => p.Close());
      _sessions.ForEach(s => s.Dispose());

      _disposed = true;
    }
  }
}