using System;
using System.Linq;
using NGIS.Serialization;
using Xunit;

namespace NGIS.Tests {
  public class MsgSerializerArrayAndStringTests {
    [Theory]
    [InlineData("Hello World!")]
    [InlineData("Привет, мир!")]
    public void ShouldSerializeAndDeserializeString(string value) {
      var buf = new byte[MsgSerializer.SizeOf(value)];
      MsgSerializer.WriteString(value, buf, 0);

      var offset = 0;
      var deserialized = MsgSerializer.ReadString(buf, ref offset);

      Assert.True(value == deserialized, $"'{value}' != '{deserialized}'");
    }

    [Fact]
    public void ShouldThrowOnNullStringSerialization() {
      Assert.Throws<NullReferenceException>(() => {
        MsgSerializer.WriteString(null, Array.Empty<byte>(), 0);
      });
    }

    [Fact]
    public void ShouldThrowOnVeryLongString() {
      Assert.Throws<OverflowException>(() => {
        const int size = MsgSerializer.MaxStringLength;
        MsgSerializer.WriteString(new string('A', size + 1), new byte[size + 2], 0);
      });
    }

    [Theory]
    [InlineData("string", 42)]
    [InlineData("Wut?!", 0)]
    [InlineData("margin", MsgSerializer.MaxArrayLength)]
    public void ShouldSerializeAndDeserializeStringArray(string item, int count) {
      var value = Enumerable.Repeat(item, count).ToArray();
      var buf = new byte[MsgSerializer.SizeOf(value)];
      MsgSerializer.WriteStringArray(value, buf, 0);

      var offset = 0;
      var deserialized = MsgSerializer.ReadStringArray(buf, ref offset);

      Assert.True(value.Length == deserialized.Length, "Length mismatch");

      for (var i = 0; i < value.Length; i++)
        Assert.True(value[i] == deserialized[i], $"Values mismatch at {i}");
    }

    [Fact]
    public void ShouldThrowOnVeryLongStringArray() {
      Assert.Throws<OverflowException>(() => {
        var longArray = Enumerable.Repeat("0", MsgSerializer.MaxArrayLength + 1).ToArray();
        MsgSerializer.WriteStringArray(longArray, Array.Empty<byte>(), 0);
      });
    }

    [Theory]
    [InlineData(42, 42)]
    [InlineData(7, 0)]
    public void ShouldSerializeAndDeserializeInt32Array(int item, int count) {
      var value = Enumerable.Repeat(item, count).ToArray();
      var buffer = new byte[MsgSerializer.SizeOf(value)];
      MsgSerializer.WriteInt32Array(value, buffer, 0);

      var offset = 0;
      var deserialized = MsgSerializer.ReadInt32Array(buffer, ref offset);

      Assert.True(value.Length == deserialized.Length, "Length mismatch");

      for (var i = 0; i < value.Length; i++)
        Assert.True(value[i] == deserialized[i], $"Values mismatch at {i}");
    }

    [Fact]
    public void ShouldThrowOnVeryLongInt32Array() {
      Assert.Throws<OverflowException>(() => {
        var longArray = Enumerable.Repeat(16, MsgSerializer.MaxArrayLength + 1).ToArray();
        MsgSerializer.WriteInt32Array(longArray, Array.Empty<byte>(), 0);
      });
    }
  }
}