using NGIS.Serialization;

namespace NGIS.Message.Server {
  public enum ServerErrorId : byte {
    InternalError,
    ProtocolError,
    ServerIsBusy,
    PlayerNameIsBusy,
    ConnectionError
  }

  public readonly struct ServerMsgError : IServerSerializableMsg {
    private const byte MsgId = (byte) ServerMsgId.Error;

    public readonly ServerErrorId ErrorId;

    public ServerMsgError(ServerErrorId errorId) {
      ErrorId = errorId;
    }

    public ServerMsgError(byte[] buffer, int offset) {
      MsgSerializer.ValidateHeader(buffer, MsgId, ref offset);
      ErrorId = (ServerErrorId) MsgSerializer.ReadByte(buffer, ref offset);
    }

    public int GetSerializedSize() {
      return MsgSerializer.HeaderLength +
             MsgSerializer.SizeOf((byte) ErrorId);
    }

    public int WriteTo(byte[] buffer, int offset) {
      var dataOffset = offset + MsgSerializer.HeaderLength;
      var written = 0;

      written += MsgSerializer.WriteByte((byte) ErrorId, buffer, dataOffset);
      written += MsgSerializer.WriteHeader(written, MsgId, buffer, offset);

      return written;
    }
  }
}