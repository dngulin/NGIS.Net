using NGIS.Serialization;

namespace NGIS.Message.Client {
  public readonly struct ClientMsgKeepAlive : IClientSerializableMsg {
    private const byte MsgId = (byte) ClientMsgId.KeepAlive;

    public int GetSerializedSize() => MsgSerializer.HeaderLength;

    public int WriteTo(byte[] buffer, int offset) {
      return MsgSerializer.WriteHeader(0, MsgId, buffer, offset);
    }
  }
}