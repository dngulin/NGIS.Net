using NGIS.Serialization;

namespace NGIS.Message.Server {
  public readonly struct ServerMsgInput : IServerSerializableMsg {
    private const byte MsgId = (byte) ServerMsgId.Inputs;

    public readonly uint Frame;
    public readonly ulong InputMask;
    public readonly byte PlayerIndex;

    public ServerMsgInput(uint frame, ulong inputMask, byte playerIndex) {
      Frame = frame;
      InputMask = inputMask;
      PlayerIndex = playerIndex;
    }

    public ServerMsgInput(byte[] buffer, int offset) {
      MsgSerializer.ValidateHeader(buffer, MsgId, ref offset);

      Frame = MsgSerializer.ReadUInt32(buffer, ref offset);
      InputMask = MsgSerializer.ReadUInt64(buffer, ref offset);
      PlayerIndex = MsgSerializer.ReadByte(buffer, ref offset);
    }

    public int GetSerializedSize() {
      return MsgSerializer.HeaderLength +
             MsgSerializer.SizeOf(Frame) +
             MsgSerializer.SizeOf(InputMask) +
             MsgSerializer.SizeOf(PlayerIndex);
    }

    public int WriteTo(byte[] buffer, int offset) {
      var msgSize = GetSerializedSize();
      MsgSerializer.WriteHeader(msgSize, MsgId, buffer, ref offset);

      MsgSerializer.WriteUInt32(Frame, buffer, ref offset);
      MsgSerializer.WriteUInt64(InputMask, buffer, ref offset);
      MsgSerializer.WriteByte(PlayerIndex, buffer, ref offset);

      return msgSize;
    }
  }
}