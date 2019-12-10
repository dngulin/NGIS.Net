using NGIS.Serialization;

namespace NGIS.Message.Server {
  public readonly struct ServerMsgKeepAlive : IServerSerializableMsg {
    private const byte MsgId = (byte) ServerMsgId.KeepAlive;

    public int GetSerializedSize() => MsgSerializer.HeaderLength;

    public int WriteTo(byte[] buffer, int offset) {
      var msgSize = GetSerializedSize();
      MsgSerializer.WriteHeader(msgSize, MsgId, buffer, ref offset);
      return msgSize;
    }
  }
}