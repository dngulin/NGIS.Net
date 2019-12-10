using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

[assembly:InternalsVisibleTo("NGIS.Tests")]
namespace NGIS.Serialization {
  internal static class MsgSerializer {
    public const int HeaderLength = sizeof(ushort) + sizeof(byte);

    public const int MaxStringLength = byte.MaxValue;
    public const int MaxArrayLength = byte.MaxValue;

    public static bool CheckBufferForMsg(byte[] buffer, int size, int offset, out int msgLength, out byte msgId) {
      msgLength = -1;
      msgId = 0;

      var available = size - offset;
      if (available < HeaderLength)
        return false;

      msgLength = ReadUInt16(buffer, ref offset);
      msgId = ReadByte(buffer, ref offset);
      return msgLength <= available;
    }

    public static void ValidateHeader(byte[] buffer, byte msgId, ref int index) {
      var available = buffer.Length - index;
      if (available < HeaderLength)
        throw new MsgSerializerException("Buffer size less then header prefix");

      var messageSize = ReadUInt16(buffer, ref index);
      if (available < messageSize)
        throw new MsgSerializerException("Buffer size less then message length");

      var actualId = ReadByte(buffer, ref index);
      if (actualId != msgId)
        throw new MsgSerializerException($"MsgId mismatch: {actualId} != {msgId}");
    }

    public static void WriteHeader(int msgLength, byte msgId, byte[] buffer, ref int offset) {
      if (msgLength < HeaderLength || msgLength > ushort.MaxValue)
        throw new MsgSerializerException("Wrong message length");

      WriteUInt16(checked((ushort) msgLength), buffer, ref offset);
      WriteByte(msgId, buffer, ref offset);
    }

    public static byte ReadByte(byte[] buffer, ref int offset) {
      return buffer[offset++];
    }

    public static void WriteByte(byte value, byte[] buffer, ref int offset) {
      buffer[offset++] = value;
    }

    public static ushort ReadUInt16(byte[] buffer, ref int offset) {
      return checked((ushort) ReadBigEndian<ushort>(buffer, ref offset));
    }

    public static void WriteUInt16(ushort value, byte[] buffer, ref int offset) {
      WriteBigEndian<ushort>(value, buffer, ref offset);
    }

    public static uint ReadUInt32(byte[] buffer, ref int offset) {
      return (uint) ReadBigEndian<uint>(buffer, ref offset);
    }

    public static void WriteUInt32(uint value, byte[] buffer, ref int offset) {
      WriteBigEndian<uint>(value, buffer, ref offset);
    }

    public static int ReadInt32(byte[] buffer, ref int offset) {
      return (int) ReadBigEndian<int>(buffer, ref offset);
    }

    public static void WriteInt32(int value, byte[] buffer, ref int offset) {
      WriteBigEndian<int>((ulong) value, buffer, ref offset);
    }

    public static ulong ReadUInt64(byte[] buffer, ref int offset) {
      return ReadBigEndian<ulong>(buffer, ref offset);
    }

    public static void WriteUInt64(ulong value, byte[] buffer, ref int offset) {
      WriteBigEndian<ulong>(value, buffer, ref offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe ulong ReadBigEndian<T>(byte[] buffer, ref int offset) where T : unmanaged {
      var value = 0UL;
      var size = sizeof(T);

      for (var i = 1; i <= size; i++)
        value |= (ulong) buffer[offset++] << ((size - i) * 8);

      return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void WriteBigEndian<T>(ulong value, byte[] buffer, ref int offset) where T : unmanaged {
      var size = sizeof(T);
      for (var i = 1; i <= size; i++)
        buffer[offset++] = (byte) (value >> ((size - i) * 8));
    }

    public static string ReadString(byte[] buffer, ref int offset) {
      var length = ReadByte(buffer, ref offset);
      var value = Encoding.UTF8.GetString(buffer, offset, length);
      offset += length;
      return value;
    }

    public static void WriteString(string value, byte[] buffer, ref int offset) {
      var length = Encoding.UTF8.GetByteCount(value);
      WriteByte(checked((byte) length), buffer, ref offset);

      var written = Encoding.UTF8.GetBytes(value, 0, value.Length, buffer, offset);
      offset += written;

      if (written != length)
        throw new MsgSerializerException("Not enough space to serialize the string!");
    }

    public static uint[] ReadUInt32Array(byte[] buffer, ref int offset) {
      var array = new uint[ReadByte(buffer, ref offset)];
      for (var i = 0; i < array.Length; i++) array[i] = ReadUInt32(buffer, ref offset);
      return array;
    }

    public static void WriteUInt32Array(uint[] values, byte[] buffer, ref int offset) {
      WriteByte(checked((byte) values.Length), buffer, ref offset);
      foreach (var value in values) WriteUInt32(value, buffer, ref offset);
    }

    public static int[] ReadInt32Array(byte[] buffer, ref int offset) {
      var array = new int[ReadByte(buffer, ref offset)];
      for (var i = 0; i < array.Length; i++) array[i] = ReadInt32(buffer, ref offset);
      return array;
    }

    public static void WriteInt32Array(int[] values, byte[] buffer, ref int offset) {
      WriteByte(checked((byte) values.Length), buffer, ref offset);
      foreach (var value in values) WriteInt32(value, buffer, ref offset);
    }

    public static string[] ReadStringArray(byte[] buffer, ref int offset) {
      var array = new string[ReadByte(buffer, ref offset)];
      for (var i = 0; i < array.Length; i++) array[i] = ReadString(buffer, ref offset);
      return array;
    }

    public static void WriteStringArray(string[] values, byte[] buffer, ref int offset) {
      WriteByte(checked((byte) values.Length), buffer, ref offset);
      foreach (var value in values) WriteString(value, buffer, ref offset);
    }

    public static int SizeOf(string value) => sizeof(byte) + Encoding.UTF8.GetByteCount(value);
    public static int SizeOf(string[] array) => sizeof(byte) + array.Sum(value => SizeOf(value));

    // ReSharper disable once UnusedParameter.Global
    public static unsafe int SizeOf<T>(T _) where T : unmanaged => sizeof(T);
    public static unsafe int SizeOf<T>(T[] array) where T : unmanaged => sizeof(byte) + sizeof(T) * array.Length;
  }
}