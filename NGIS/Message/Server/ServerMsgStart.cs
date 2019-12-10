using NGIS.Serialization;

namespace NGIS.Message.Server {
  public readonly struct ServerMsgStart : IServerSerializableMsg {
    private const byte MsgId = (byte) ServerMsgId.Start;

    public readonly int Seed;
    public readonly string[] Players;
    public readonly byte YourIndex;
    public readonly byte TicksPerSecond;

    public ServerMsgStart(int seed, string[] players, byte yourIndex, byte ticksPerSecond) {
      Seed = seed;
      Players = players;
      YourIndex = yourIndex;
      TicksPerSecond = ticksPerSecond;
    }

    public ServerMsgStart(byte[] buffer, int offset) {
      MsgSerializer.ValidateHeader(buffer, MsgId, ref offset);

      Seed = MsgSerializer.ReadInt32(buffer, ref offset);
      Players = MsgSerializer.ReadStringArray(buffer, ref offset);
      YourIndex = MsgSerializer.ReadByte(buffer, ref offset);
      TicksPerSecond = MsgSerializer.ReadByte(buffer, ref offset);
    }

    public int GetSerializedSize() {
      return MsgSerializer.HeaderLength +
             MsgSerializer.SizeOf(Seed) +
             MsgSerializer.SizeOf(Players) +
             MsgSerializer.SizeOf(YourIndex) +
             MsgSerializer.SizeOf(TicksPerSecond);
    }

    public int WriteTo(byte[] buffer, int offset) {
      var msgSize = GetSerializedSize();
      MsgSerializer.WriteHeader(msgSize, MsgId, buffer, ref offset);

      MsgSerializer.WriteInt32(Seed, buffer, ref offset);
      MsgSerializer.WriteStringArray(Players, buffer, ref offset);
      MsgSerializer.WriteByte(YourIndex, buffer, ref offset);
      MsgSerializer.WriteByte(TicksPerSecond, buffer, ref offset);

      return msgSize;
    }
  }
}