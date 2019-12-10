using System;
using System.Diagnostics;
using System.Net.Sockets;
using NGIS.Message;
using NGIS.Serialization;

namespace NGIS.Pipe {
  public abstract class AbstractMsgPipe {
    private const int SendIterationsLimit = 10;

    private readonly Socket _socket;

    private readonly byte[] _buffer = new byte[1024];
    private int _received;

    private readonly Stopwatch _sendTimer;
    private readonly Stopwatch _receiveTimer;

    protected AbstractMsgPipe(Socket socket) {
      _socket = socket;
      _socket.NoDelay = true;

      _sendTimer = Stopwatch.StartNew();
      _receiveTimer = Stopwatch.StartNew();
    }
    public long TimeSinceLastSend => _sendTimer.ElapsedMilliseconds;
    public long TimeSinceLastReceive => _receiveTimer.ElapsedMilliseconds;

    public bool Closed { get; private set; }

    public bool IsConnected() {
      if (!_socket.Connected || Closed)
        return false;

      return _socket.Available > 0 || !_socket.Poll(1000, SelectMode.SelectRead);
    }

    public void Close() {
      try {
        _socket.Shutdown(SocketShutdown.Both);
      }
      finally {
        _socket.Close();
      }
      Closed = true;
    }

    protected void SendMessage<T>(T msg, byte[] sendBuffer) where T : struct, ISerializableMsg {
      if (sendBuffer.Length < msg.GetSerializedSize())
        throw new MsgPipeException("Send buffer too small");

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