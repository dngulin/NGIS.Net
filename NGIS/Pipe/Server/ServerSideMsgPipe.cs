using System.Collections.Generic;
using System.Net.Sockets;
using NGIS.Message.Client;
using NGIS.Message.Server;

namespace NGIS.Pipe.Server {
  public class ServerSideMsgPipe : AbstractMsgPipe {
    public ServerSideMsgPipe(Socket socket) : base(socket) {
    }

    public readonly Queue<ClientMsgId> ReceiveOrder = new Queue<ClientMsgId>(32);

    public readonly Queue<ClientMsgJoin> JoinMessages = new Queue<ClientMsgJoin>(1);
    public readonly Queue<ClientMsgInputs> InputMessages = new Queue<ClientMsgInputs>(16);
    public readonly Queue<ClientMsgFinished> FinishedMessages = new Queue<ClientMsgFinished>(1);

    public void WriteToBufferAndSend<T>(T msg, byte[] sendBuffer) where T : struct, IServerSerializableMsg {
      SendMessage(msg, sendBuffer);
    }

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