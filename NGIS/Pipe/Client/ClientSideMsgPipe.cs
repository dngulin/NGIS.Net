using System.Collections.Generic;
using System.Net.Sockets;
using NGIS.Message.Client;
using NGIS.Message.Server;
using NGIS.Session;

namespace NGIS.Pipe.Client {
  public class ClientSideMsgPipe : AbstractMsgPipe {
    public ClientSideMsgPipe(Socket socket) : base(socket) { }

    public readonly Queue<ServerMsgId> ReceiveOrder = new Queue<ServerMsgId>(32);

    public readonly Queue<ServerMsgError> ErrorMessages = new Queue<ServerMsgError>(1);
    public readonly Queue<ServerMsgStart> StartMessages = new Queue<ServerMsgStart>(1);
    public readonly Queue<ServerMsgInput> InputMessages = new Queue<ServerMsgInput>(16);
    public readonly Queue<ServerMsgFinish> FinishMessages = new Queue<ServerMsgFinish>(1);

    protected override void ReadMsg(byte msgId, byte[] buffer, int offset) {
      var id = (ServerMsgId) msgId;
      ReceiveOrder.Enqueue(id);

      switch (id) {
        case ServerMsgId.KeepAlive:
        case ServerMsgId.Joined:
          break;

        case ServerMsgId.Error:
          ErrorMessages.Enqueue(new ServerMsgError(buffer, offset));
          break;
        case ServerMsgId.Start:
          StartMessages.Enqueue(new ServerMsgStart(buffer, offset));
          break;
        case ServerMsgId.Inputs:
          InputMessages.Enqueue(new ServerMsgInput(buffer, offset));
          break;
        case ServerMsgId.Finish:
          FinishMessages.Enqueue(new ServerMsgFinish(buffer, offset));
          break;

        default:
          throw new ProtocolException($"Unknown message id: {msgId}");
      }
    }

    public void SendMessageUsingBuffer<T>(T msg, byte[] sendBuffer) where T : struct, IClientSerializableMsg {
      SendMessage(msg, sendBuffer);
    }
  }
}