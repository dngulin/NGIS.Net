using NGIS.Serialization;

namespace NGIS.Message.Server {
  public readonly struct ServerMsgKeepAlive : IServerSerializableMsg {
    private const byte MsgId = (byte) ServerMsgId.KeepAlive;

    public int GetSerializedSize() => MsgSerializer.HeaderLength;

    public int WriteTo(byte[] buffer, int offset) {
      return MsgSerializer.WriteHeader(0, MsgId, buffer, offset);
    }
  }
}