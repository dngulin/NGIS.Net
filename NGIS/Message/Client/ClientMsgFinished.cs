using NGIS.Serialization;

namespace NGIS.Message.Client {
  public struct ClientMsgFinished : IClientSerializableMsg {
    private const byte MsgId = (byte) ClientMsgId.Finished;

    public readonly uint Frame;
    public readonly int StateHash;

    public ClientMsgFinished(uint frame, int stateHash) {
      Frame = frame;
      StateHash = stateHash;
    }

    public ClientMsgFinished(byte[] buffer, int offset) {
      MsgSerializer.ValidateHeader(buffer, MsgId, ref offset);

      Frame = MsgSerializer.ReadUInt32(buffer, ref offset);
      StateHash = MsgSerializer.ReadInt32(buffer, ref offset);
    }

    public int GetSerializedSize() {
      return MsgSerializer.HeaderLength +
             MsgSerializer.SizeOf(Frame) +
             MsgSerializer.SizeOf(StateHash);
    }

    public int WriteTo(byte[] buffer, int offset) {
      var dataOffset = offset + MsgSerializer.HeaderLength;
      var written = 0;

      written += MsgSerializer.WriteUInt32(Frame, buffer, dataOffset);
      written += MsgSerializer.WriteInt32(StateHash, buffer, dataOffset + written);
      written += MsgSerializer.WriteHeader(written, MsgId, buffer, offset);

      return written;
    }
  }
}