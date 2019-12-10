using NGIS.Serialization;

namespace NGIS.Message.Server {
  public readonly struct ServerMsgJoined : IServerSerializableMsg {
    private const byte MsgId = (byte) ServerMsgId.Joined;

    public int GetSerializedSize() => MsgSerializer.HeaderLength;

    public int WriteTo(byte[] buffer, int offset) {
      return MsgSerializer.WriteHeader(0, MsgId, buffer, offset);
    }
  }
}