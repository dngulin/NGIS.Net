using NGIS.Serialization;

namespace NGIS.Message.Client {
  public struct ClientMsgInputs : IClientSerializableMsg {
    private const byte MsgId = (byte) ClientMsgId.Inputs;

    public readonly uint Frame;
    public readonly ulong InputMask;

    public ClientMsgInputs(uint frame, ulong inputMask) {
      Frame = frame;
      InputMask = inputMask;
    }

    public ClientMsgInputs(byte[] buffer, int offset) {
      MsgSerializer.ValidateHeader(buffer, MsgId, ref offset);

      Frame = MsgSerializer.ReadUInt32(buffer, ref offset);
      InputMask = MsgSerializer.ReadUInt64(buffer, ref offset);
    }

    public int GetSerializedSize() {
      return MsgSerializer.HeaderLength +
             MsgSerializer.SizeOf(Frame) +
             MsgSerializer.SizeOf(InputMask);
    }

    public int WriteTo(byte[] buffer, int offset) {
      var msgSize = GetSerializedSize();
      MsgSerializer.WriteHeader(msgSize, MsgId, buffer, ref offset);

      MsgSerializer.WriteUInt32(Frame, buffer, ref offset);
      MsgSerializer.WriteUInt64(InputMask, buffer, ref offset);

      return msgSize;
    }
  }
}