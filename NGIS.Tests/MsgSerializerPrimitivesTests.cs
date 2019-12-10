using NGIS.Serialization;
using Xunit;

namespace NGIS.Tests {
  public class MsgSerializerPrimitivesTests {
    private readonly byte[] _buffer = new byte[sizeof(ulong)];

    [Fact]
    public void ShouldSerializeAndDeserializeByte() {
      for (int i = byte.MinValue; i <= byte.MaxValue; i++) {
        var value = (byte) i;

        var written = MsgSerializer.WriteByte(value, _buffer, 0);
        Assert.True(written == sizeof(byte), $"Bad write size: {written}");

        var offset = 0;
        var deserialized = MsgSerializer.ReadByte(_buffer, ref offset);
        Assert.True(offset == sizeof(byte), $"Bad read size: {offset}");

        Assert.True(value == deserialized, $"Bad result: {deserialized}");
      }
    }

    [Theory]
    [InlineData(ushort.MinValue)]
    [InlineData(ushort.MaxValue)]
    [InlineData((ushort) (ushort.MaxValue / 2))]
    [InlineData((ushort) (ushort.MaxValue / 3))]
    [InlineData((ushort) (ushort.MaxValue / 17))]
    public void ShouldSerializeAndDeserializeUInt16(ushort value) {
      var written = MsgSerializer.WriteUInt16(value, _buffer, 0);
      Assert.True(written == sizeof(ushort), $"Bad write size: {written}");

      var offset = 0;
      var deserialized = MsgSerializer.ReadUInt16(_buffer, ref offset);
      Assert.True(offset == sizeof(ushort), $"Bad read size: {offset}");

      Assert.True(value == deserialized, $"Bad result: {deserialized}");
    }

    [Theory]
    [InlineData(uint.MinValue)]
    [InlineData(uint.MaxValue)]
    [InlineData(uint.MaxValue / 2)]
    [InlineData(uint.MaxValue / 3)]
    [InlineData(uint.MaxValue / 17)]
    public void ShouldSerializeAndDeserializeUInt32(uint value) {
      var written = MsgSerializer.WriteUInt32(value, _buffer, 0);
      Assert.True(written == sizeof(uint), $"Bad write size: {written}");

      var offset = 0;
      var deserialized = MsgSerializer.ReadUInt32(_buffer, ref offset);
      Assert.True(offset == sizeof(uint), $"Bad read size: {offset}");

      Assert.True(value == deserialized, $"Bad result: {deserialized}");
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(0)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MaxValue / 2)]
    [InlineData(int.MinValue / 2)]
    [InlineData(int.MaxValue / 3)]
    [InlineData(int.MinValue / 3)]
    [InlineData(int.MaxValue / 17)]
    [InlineData(int.MinValue / 17)]
    public void ShouldSerializeAndDeserializeInt32(int value) {
      var written = MsgSerializer.WriteInt32(value, _buffer, 0);
      Assert.True(written == sizeof(int), $"Bad write size: {written}");

      var offset = 0;
      var deserialized = MsgSerializer.ReadInt32(_buffer, ref offset);
      Assert.True(offset == sizeof(int), $"Bad read size: {offset}");

      Assert.True(value == deserialized, $"Bad result: {deserialized}");
    }

    [Theory]
    [InlineData(ulong.MinValue)]
    [InlineData(ulong.MaxValue)]
    [InlineData(ulong.MaxValue / 2)]
    [InlineData(ulong.MaxValue / 3)]
    [InlineData(ulong.MaxValue / 17)]
    public void ShouldSerializeAndDeserializeUInt64(ulong value) {
      var written = MsgSerializer.WriteUInt64(value, _buffer, 0);
      Assert.True(written == sizeof(ulong), $"Bad write size: {written}");

      var offset = 0;
      var deserialized = MsgSerializer.ReadUInt64(_buffer, ref offset);
      Assert.True(offset == sizeof(ulong), $"Bad read size: {offset}");

      Assert.True(value == deserialized, $"Bad result: {deserialized}");
    }
  }
}