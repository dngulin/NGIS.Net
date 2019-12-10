using System;
using System.Net;
using System.Net.Sockets;
using NGIS.Message.Client;
using NGIS.Message.Server;
using NGIS.Pipe.Client;
using NGIS.Pipe.Server;
using Xunit;

namespace NGIS.Tests {
  public class MsgPipeTransferTests : IDisposable {
    private readonly ServerSideMsgPipe _serverSidePipe;
    private readonly ClientSideMsgPipe _clientSidePipe;

    private readonly byte[] _sendBuffer = new byte[1024];

    public MsgPipeTransferTests() {
      var ip = IPAddress.Loopback;

      var serverSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      serverSocket.Bind(new IPEndPoint(ip, 0));
      serverSocket.Listen(1);

      var clientSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      clientSocket.Connect(serverSocket.LocalEndPoint);

      _serverSidePipe = new ServerSideMsgPipe(serverSocket.Accept());
      _clientSidePipe = new ClientSideMsgPipe(clientSocket);
    }

    public void Dispose() {
      _serverSidePipe?.Close();
      _clientSidePipe?.Close();
    }

    [Fact]
    public void ServerShouldReceiveAllClientMessages() {
      var msgOrder = new[] {ClientMsgId.KeepAlive, ClientMsgId.Join, ClientMsgId.Inputs, ClientMsgId.Finished};

      _clientSidePipe.SendMessageUsingBuffer(new ClientMsgKeepAlive(), _sendBuffer);
      _clientSidePipe.SendMessageUsingBuffer(new ClientMsgJoin("game", "player", 0), _sendBuffer);
      _serverSidePipe.ReceiveMessages();

      _clientSidePipe.SendMessageUsingBuffer(new ClientMsgInputs(1, 2), _sendBuffer);
      _clientSidePipe.SendMessageUsingBuffer(new ClientMsgFinished(3, 4), _sendBuffer);
      _serverSidePipe.ReceiveMessages();

      Assert.True(_serverSidePipe.ReceiveOrder.Count == msgOrder.Length);
      foreach (var msgId in msgOrder) {
        Assert.True(msgId == _serverSidePipe.ReceiveOrder.Dequeue());

        switch (msgId) {
          case ClientMsgId.KeepAlive:
            break;
          case ClientMsgId.Join:
            _serverSidePipe.JoinMessages.Dequeue();
            break;
          case ClientMsgId.Inputs:
            _serverSidePipe.InputMessages.Dequeue();
            break;
          case ClientMsgId.Finished:
            _serverSidePipe.FinishedMessages.Dequeue();
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }
      }
    }

    [Fact]
    public void ClientShouldReceiveAllServerMessages() {
      var msgOrder = new[] {
        ServerMsgId.KeepAlive, ServerMsgId.Error, ServerMsgId.Joined,
        ServerMsgId.Start, ServerMsgId.Inputs, ServerMsgId.Finish
      };

      _serverSidePipe.SendMessageUsingBuffer(new ServerMsgKeepAlive(), _sendBuffer);
      _serverSidePipe.SendMessageUsingBuffer(new ServerMsgError(ServerErrorId.InternalError), _sendBuffer);
      _serverSidePipe.SendMessageUsingBuffer(new ServerMsgJoined(), _sendBuffer);
      _clientSidePipe.ReceiveMessages();

      _serverSidePipe.SendMessageUsingBuffer(new ServerMsgStart(0, new []{"foo", "bar"}, 1, 25), _sendBuffer);
      _serverSidePipe.SendMessageUsingBuffer(new ServerMsgInput(3, 2, 1), _sendBuffer);
      _serverSidePipe.SendMessageUsingBuffer(new ServerMsgFinish(new []{0u, 1u}, new []{2, 3}), _sendBuffer);
      _clientSidePipe.ReceiveMessages();

      Assert.True(_clientSidePipe.ReceiveOrder.Count == msgOrder.Length);
      foreach (var msgId in msgOrder) {
        Assert.True(msgId == _clientSidePipe.ReceiveOrder.Dequeue());

        switch (msgId) {
          case ServerMsgId.KeepAlive:
          case ServerMsgId.Joined:
            break;

          case ServerMsgId.Error:
            _clientSidePipe.ErrorMessages.Dequeue();
            break;
          case ServerMsgId.Start:
            _clientSidePipe.StartMessages.Dequeue();
            break;
          case ServerMsgId.Inputs:
            _clientSidePipe.InputMessages.Dequeue();
            break;
          case ServerMsgId.Finish:
            _clientSidePipe.FinishMessages.Dequeue();
            break;

          default:
            throw new ArgumentOutOfRangeException();
        }
      }
    }
  }
}