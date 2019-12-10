using NGIS.Serialization;

namespace NGIS.Message.Server {
  public readonly struct ServerMsgJoined : IServerSerializableMsg {
    private const byte MsgId = (byte) ServerMsgId.Joined;

    public int GetSerializedSize() => MsgSerializer.HeaderLength;

    public int WriteTo(byte[] buffer, int offset) {
      var msgSize = GetSerializedSize();
      MsgSerializer.WriteHeader(msgSize, MsgId, buffer, ref offset);
      return msgSize;
    }
  }
}