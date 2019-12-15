using NGIS.Serialization;

namespace NGIS.Message.Client {
  public struct ClientMsgJoin : IClientSerializableMsg {
    private const byte MsgId = (byte) ClientMsgId.Join;

    public readonly string Game;
    public readonly ushort Version;
    public readonly string PlayerName;

    public ClientMsgJoin(string game, ushort version, string playerName) {
      Game = game;
      Version = version;
      PlayerName = playerName;
    }

    public ClientMsgJoin(byte[] buffer, int offset) {
      MsgSerializer.ValidateHeader(buffer, MsgId, ref offset);

      Game = MsgSerializer.ReadString(buffer, ref offset);
      Version = MsgSerializer.ReadUInt16(buffer, ref offset);
      PlayerName = MsgSerializer.ReadString(buffer, ref offset);
    }

    public int GetSerializedSize() {
      return MsgSerializer.HeaderLength +
             MsgSerializer.SizeOf(Game) +
             MsgSerializer.SizeOf(Version) +
             MsgSerializer.SizeOf(PlayerName);
    }

    public int WriteTo(byte[] buffer, int offset) {
      var dataOffset = offset + MsgSerializer.HeaderLength;
      var written = 0;

      written += MsgSerializer.WriteString(Game, buffer, dataOffset);
      written += MsgSerializer.WriteUInt16(Version, buffer, dataOffset + written);
      written += MsgSerializer.WriteString(PlayerName, buffer, dataOffset + written);
      written += MsgSerializer.WriteHeader(written, MsgId, buffer, offset);

      return written;
    }
  }
}