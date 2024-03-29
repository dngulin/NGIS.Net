using System.Collections.Generic;
using System.Net.Sockets;
using NGIS.Message.Client;
using NGIS.Message.Server;

namespace NGIS.Pipe.Server {
  public class ServerSideMsgPipe : AbstractMsgPipe<IServerSerializableMsg> {
    public ServerSideMsgPipe(Socket socket, int receiveBufferSize) : base(socket, receiveBufferSize) {
      Id = socket.RemoteEndPoint.ToString();
    }

    public readonly Queue<ClientMsgId> ReceiveOrder = new Queue<ClientMsgId>(32);

    public readonly Queue<ClientMsgJoin> JoinMessages = new Queue<ClientMsgJoin>(1);
    public readonly Queue<ClientMsgInputs> InputMessages = new Queue<ClientMsgInputs>(16);
    public readonly Queue<ClientMsgFinished> FinishedMessages = new Queue<ClientMsgFinished>(1);
    public string Id { get; }

    protected override void ReadMsg(byte msgId, byte[] buffer, int offset) {
      var id = (ClientMsgId) msgId;
      ReceiveOrder.Enqueue(id);

      switch (id) {
        case ClientMsgId.KeepAlive:
          break;

        case ClientMsgId.Join:
          JoinMessages.Enqueue(new ClientMsgJoin(buffer, offset));
          break;
        case ClientMsgId.Inputs:
          InputMessages.Enqueue(new ClientMsgInputs(buffer, offset));
          break;
        case ClientMsgId.Finished:
          FinishedMessages.Enqueue(new ClientMsgFinished(buffer, offset));
          break;

        default:
          throw new MsgPipeException($"Unknown message id: {msgId}");
      }
    }
  }
}