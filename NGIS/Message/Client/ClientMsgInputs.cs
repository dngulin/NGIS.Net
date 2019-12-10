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
      var dataOffset = offset + MsgSerializer.HeaderLength;
      var written = 0;

      written += MsgSerializer.WriteUInt32(Frame, buffer, dataOffset);
      written += MsgSerializer.WriteUInt64(InputMask, buffer, dataOffset + written);
      written += MsgSerializer.WriteHeader(written, MsgId, buffer, offset);

      return written;
    }
  }
}