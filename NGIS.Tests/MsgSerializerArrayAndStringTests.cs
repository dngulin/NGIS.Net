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
      var index = 0;
      MsgSerializer.WriteString(value, buf, ref index);

      index = 0;
      var deserialized = MsgSerializer.ReadString(buf, ref index);

      Assert.True(value == deserialized, $"'{value}' != '{deserialized}'");
    }

    [Fact]
    public void ShouldThrowOnNullStringSerialization() {
      var buf = Array.Empty<byte>();
      var index = 0;
      Assert.Throws<ArgumentNullException>(() => MsgSerializer.WriteString(null, buf, ref index));
    }

    [Fact]
    public void ShouldThrowOnVeryLongString() {
      var buf = Array.Empty<byte>();
      var longString = new string('F', MsgSerializer.MaxStringLength + 1);
      var index = 0;

      Assert.Throws<OverflowException>(() => MsgSerializer.WriteString(longString, buf, ref index));
    }

    [Theory]
    [InlineData("string", 42)]
    [InlineData("Wut?!", 0)]
    [InlineData("margin", MsgSerializer.MaxArrayLength)]
    public void ShouldSerializeAndDeserializeStringArray(string item, int count) {
      var value = Enumerable.Repeat(item, count).ToArray();
      var buf = new byte[MsgSerializer.SizeOf(value)];
      var index = 0;
      MsgSerializer.WriteStringArray(value, buf, ref index);

      index = 0;
      var deserialized = MsgSerializer.ReadStringArray(buf, ref index);

      Assert.True(value.Length == deserialized.Length, "Length mismatch");

      for (var i = 0; i < value.Length; i++)
        Assert.True(value[i] == deserialized[i], $"Values mismatch at {i}");
    }

    [Fact]
    public void ShouldThrowOnVeryLongStringArray() {
      var longArray = Enumerable.Repeat("0", MsgSerializer.MaxArrayLength + 1).ToArray();
      var buf = Array.Empty<byte>();
      var index = 0;

      Assert.Throws<OverflowException>(() => MsgSerializer.WriteStringArray(longArray, buf, ref index));
    }

    [Theory]
    [InlineData(42, 42)]
    [InlineData(7, 0)]
    public void ShouldSerializeAndDeserializeInt32Array(int item, int count) {
      var value = Enumerable.Repeat(item, count).ToArray();
      var buffer = new byte[MsgSerializer.SizeOf(value)];
      var index = 0;
      MsgSerializer.WriteInt32Array(value, buffer, ref index);

      index = 0;
      var deserialized = MsgSerializer.ReadInt32Array(buffer, ref index);

      Assert.True(value.Length == deserialized.Length, "Length mismatch");

      for (var i = 0; i < value.Length; i++)
        Assert.True(value[i] == deserialized[i], $"Values mismatch at {i}");
    }

    [Fact]
    public void ShouldThrowOnVeryLongInt32Array() {
      var longArray = Enumerable.Repeat(16, MsgSerializer.MaxArrayLength + 1).ToArray();
      var buf = Array.Empty<byte>();
      var index = 0;

      Assert.Throws<OverflowException>(() => MsgSerializer.WriteInt32Array(longArray, buf, ref index));
    }
  }
}