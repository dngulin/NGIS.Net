using NGIS.Serialization;

namespace NGIS.Message.Client {
  public struct ClientMsgJoin : IClientSerializableMsg {
    private const byte MsgId = (byte) ClientMsgId.Join;

    public readonly string GameName;
    public readonly string PlayerName;
    public readonly ushort ProtocolVersion;

    public ClientMsgJoin(string gameName, string playerName, ushort protocolVersion) {
      GameName = gameName;
      PlayerName = playerName;
      ProtocolVersion = protocolVersion;
    }

    public ClientMsgJoin(byte[] buffer, int offset) {
      MsgSerializer.ValidateHeader(buffer, MsgId, ref offset);

      GameName = MsgSerializer.ReadString(buffer, ref offset);
      PlayerName = MsgSerializer.ReadString(buffer, ref offset);
      ProtocolVersion = MsgSerializer.ReadUInt16(buffer, ref offset);
    }

    public int GetSerializedSize() {
      return MsgSerializer.HeaderLength +
             MsgSerializer.SizeOf(GameName) +
             MsgSerializer.SizeOf(PlayerName) +
             MsgSerializer.SizeOf(ProtocolVersion);
    }

    public int WriteTo(byte[] buffer, int offset) {
      var msgSize = GetSerializedSize();
      MsgSerializer.WriteHeader(msgSize, MsgId, buffer, ref offset);

      MsgSerializer.WriteString(GameName, buffer, ref offset);
      MsgSerializer.WriteString(PlayerName, buffer, ref offset);
      MsgSerializer.WriteUInt16(ProtocolVersion, buffer, ref offset);

      return msgSize;
    }
  }
}