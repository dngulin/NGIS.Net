using NGIS.Message.Client;
using NGIS.Message.Server;

namespace NGIS.Message {
  public static class MsgExtensions {
    public static ServerMsgInput ToServerMsg(this ClientMsgInputs msg, byte playerIndex) {
      return new ServerMsgInput(msg.Frame, msg.InputMask, playerIndex);
    }
  }
}