using NGIS.Serialization;
using Xunit;

namespace NGIS.Tests {
  public class MsgSerializerPrimitivesTests {
    private readonly byte[] _buffer = new byte[sizeof(ulong)];

    [Fact]
    public void ShouldSerializeAndDeserializeByte() {
      for (int i = byte.MinValue; i <= byte.MaxValue; i++) {
        var index = 0;
        var value = (byte) i;

        MsgSerializer.WriteByte(value, _buffer, ref index);
        Assert.True(index == sizeof(byte), $"Bad write size: {index}");

        index = 0;
        var deserialized = MsgSerializer.ReadByte(_buffer, ref index);
        Assert.True(index == sizeof(byte), $"Bad read size: {index}");

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
      var index = 0;
      MsgSerializer.WriteUInt16(value, _buffer, ref index);
      Assert.True(index == sizeof(ushort), $"Bad write size: {index}");

      index = 0;
      var deserialized = MsgSerializer.ReadUInt16(_buffer, ref index);
      Assert.True(index == sizeof(ushort), $"Bad read size: {index}");

      Assert.True(value == deserialized, $"Bad result: {deserialized}");
    }

    [Theory]
    [InlineData(uint.MinValue)]
    [InlineData(uint.MaxValue)]
    [InlineData(uint.MaxValue / 2)]
    [InlineData(uint.MaxValue / 3)]
    [InlineData(uint.MaxValue / 17)]
    public void ShouldSerializeAndDeserializeUInt32(uint value) {
      var index = 0;
      MsgSerializer.WriteUInt32(value, _buffer, ref index);
      Assert.True(index == sizeof(uint), $"Bad write size: {index}");

      index = 0;
      var deserialized = MsgSerializer.ReadUInt32(_buffer, ref index);
      Assert.True(index == sizeof(uint), $"Bad read size: {index}");

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
      var index = 0;
      MsgSerializer.WriteInt32(value, _buffer, ref index);
      Assert.True(index == sizeof(int), $"Bad write size: {index}");

      index = 0;
      var deserialized = MsgSerializer.ReadInt32(_buffer, ref index);
      Assert.True(index == sizeof(int), $"Bad read size: {index}");

      Assert.True(value == deserialized, $"Bad result: {deserialized}");
    }

    [Theory]
    [InlineData(ulong.MinValue)]
    [InlineData(ulong.MaxValue)]
    [InlineData(ulong.MaxValue / 2)]
    [InlineData(ulong.MaxValue / 3)]
    [InlineData(ulong.MaxValue / 17)]
    public void ShouldSerializeAndDeserializeUInt64(ulong value) {
      var index = 0;
      MsgSerializer.WriteUInt64(value, _buffer, ref index);
      Assert.True(index == sizeof(ulong), $"Bad write size: {index}");

      index = 0;
      var deserialized = MsgSerializer.ReadUInt64(_buffer, ref index);
      Assert.True(index == sizeof(ulong), $"Bad read size: {index}");

      Assert.True(value == deserialized, $"Bad result: {deserialized}");
    }
  }
}