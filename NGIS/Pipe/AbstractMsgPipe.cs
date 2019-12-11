using System;
using System.Diagnostics;
using System.Net.Sockets;
using NGIS.Message;
using NGIS.Serialization;

namespace NGIS.Pipe {
  public abstract class AbstractMsgPipe<TMsgBase> where TMsgBase : ISerializableMsg {
    private const int SendIterationsLimit = 10;
    private const int KeepAlivePeriod = 250;
    private const int ReceiveTimeout = KeepAlivePeriod * 5;

    private readonly Socket _socket;

    private readonly byte[] _buffer;
    private int _received;

    private readonly Stopwatch _sendTimer;
    private readonly Stopwatch _receiveTimer;

    protected AbstractMsgPipe(Socket socket, int receiveBufferSize) {
      _socket = socket;
      _socket.NoDelay = true;

      _buffer = new byte[receiveBufferSize];

      _sendTimer = Stopwatch.StartNew();
      _receiveTimer = Stopwatch.StartNew();
    }

    public bool IsKeepAliveTimeout() => _sendTimer.ElapsedMilliseconds > KeepAlivePeriod;
    public bool IsReceiveTimeout() => _receiveTimer.ElapsedMilliseconds > ReceiveTimeout;

    public bool Closed { get; private set; }

    public bool IsConnected() {
      if (!_socket.Connected || Closed)
        return false;

      return _socket.Available > 0 || !_socket.Poll(500, SelectMode.SelectRead);
    }

    public void Close() {
      try {
        _socket.Shutdown(SocketShutdown.Both);
      }
      finally {
        _socket.Close();
        Closed = true;
      }
    }

    public void SendMessageUsingBuffer<TMsg>(TMsg msg, byte[] sendBuffer) where TMsg : struct, TMsgBase {
      var msgSize = msg.WriteTo(sendBuffer, 0);

      var bytesSent = 0;
      var iteration = 0;

      while (bytesSent < msgSize) {
        if (iteration++ > SendIterationsLimit)
          throw new MsgPipeException($"Failed to send message in {SendIterationsLimit} iterations");

        bytesSent += _socket.Send(sendBuffer, bytesSent, msgSize - bytesSent, SocketFlags.None);
      }

      _sendTimer.Restart();
    }

    public void ReceiveMessages() {
      while (_socket.Available > 0) {
        var size = Math.Min(_buffer.Length - _received, _socket.Available);

        _received += _socket.Receive(_buffer, _received, size, SocketFlags.None);

        var offset = 0;
        while (MsgSerializer.CheckBufferForMsg(_buffer, _received, offset, out var msgSize, out var msgId)) {
          ReadMsg(msgId, _buffer, offset);
          offset += msgSize;
          _receiveTimer.Restart();
        }

        Array.Copy(_buffer, offset, _buffer, 0, _received - offset);
        _received -= offset;

        if (_received >= _buffer.Length)
          throw new MsgPipeException("Failed to receive message: buffer too small");
      }
    }

    protected abstract void ReadMsg(byte msgId, byte[] buffer, int offset);
  }
}