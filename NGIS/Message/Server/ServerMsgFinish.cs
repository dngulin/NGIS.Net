using System;
using NGIS.Serialization;

namespace NGIS.Message.Server {
  public readonly struct ServerMsgFinish : IServerSerializableMsg {
    private const byte MsgId = (byte) ServerMsgId.Finish;

    public readonly uint[] Frames;
    public readonly int[] Hashes;

    public ServerMsgFinish(uint[] frames, int[] hashes) {
      if (frames.Length != hashes.Length)
        throw new InvalidOperationException("Frames and hashes should have same length");

      Frames = frames;
      Hashes = hashes;
    }

    public ServerMsgFinish(byte[] buffer, int offset) {
      MsgSerializer.ValidateHeader(buffer, MsgId, ref offset);

      Frames = MsgSerializer.ReadUInt32Array(buffer, ref offset);
      Hashes = MsgSerializer.ReadInt32Array(buffer, ref offset);
    }

    public int GetSerializedSize() {
      return MsgSerializer.HeaderLength +
             MsgSerializer.SizeOf(Frames) +
             MsgSerializer.SizeOf(Hashes);
    }

    public int WriteTo(byte[] buffer, int offset) {
      var dataOffset = offset + MsgSerializer.HeaderLength;
      var written = 0;

      written += MsgSerializer.WriteUInt32Array(Frames, buffer, dataOffset);
      written += MsgSerializer.WriteInt32Array(Hashes, buffer, dataOffset + written);
      written += MsgSerializer.WriteHeader(written, MsgId, buffer, offset);

      return written;
    }
  }
}