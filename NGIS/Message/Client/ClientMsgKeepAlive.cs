using NGIS.Serialization;

namespace NGIS.Message.Client {
  public readonly struct ClientMsgKeepAlive : IClientSerializableMsg {
    private const byte MsgId = (byte) ClientMsgId.KeepAlive;

    public int GetSerializedSize() => MsgSerializer.HeaderLength;

    public int WriteTo(byte[] buffer, int offset) {
      var msgSize = GetSerializedSize();
      MsgSerializer.WriteHeader(msgSize, MsgId, buffer, ref offset);
      return msgSize;
    }
  }
}