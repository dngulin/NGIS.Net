using NGIS.Message.Server;
using NGIS.Serialization;
using Xunit;

namespace NGIS.Tests {
  public class ServerMessageSerializationTest {
    private static byte[] CreateBuffer<T>(T msg) where T : struct, IServerSerializableMsg {
      return new byte[msg.GetSerializedSize()];
    }

    [Fact]
    public void ShouldSerializeKeepAlive() {
      var msg = new ServerMsgKeepAlive();

      var buf = CreateBuffer(msg);
      var written = msg.WriteTo(buf, 0);
      Assert.True(written == buf.Length);

      var idx = 0;
      MsgSerializer.ValidateHeader(buf, (byte) ServerMsgId.KeepAlive, ref idx);
    }

    [Theory]
    [InlineData(ServerErrorId.ProtocolError)]
    [InlineData(ServerErrorId.ServerIsBusy)]
    [InlineData(ServerErrorId.PlayerNameIsBusy)]
    [InlineData(ServerErrorId.ConnectionError)]
    public void ShouldSerializeAndDeserializeError(ServerErrorId errorId) {
      var originalMsg = new ServerMsgError(errorId);

      var buf = CreateBuffer(originalMsg);
      var written = originalMsg.WriteTo(buf, 0);
      Assert.True(written == buf.Length);

      var restoredMsg = new ServerMsgError(buf, 0);

      Assert.True(originalMsg.ErrorId == restoredMsg.ErrorId);
    }

    [Fact]
    public void ShouldSerializeJoined() {
      var msg = new ServerMsgJoined();

      var buf = CreateBuffer(msg);
      var written = msg.WriteTo(buf, 0);
      Assert.True(written == buf.Length);

      var idx = 0;
      MsgSerializer.ValidateHeader(buf, (byte) ServerMsgId.Joined, ref idx);
    }

    [Theory]
    [InlineData(2345, new [] {"Player A", "Player B"}, 0, 20)]
    [InlineData(4578, new [] {"Player C", "Player D"}, 1, 30)]
    public void ShouldSerializeAndDeserializeStart(int seed, string[] players, byte yourIndex, byte tps) {
      var originalMsg = new ServerMsgStart(seed, players, yourIndex, tps);

      var buf = CreateBuffer(originalMsg);
      var written = originalMsg.WriteTo(buf, 0);
      Assert.True(written == buf.Length);

      var restoredMsg = new ServerMsgStart(buf, 0);

      Assert.True(originalMsg.Seed == restoredMsg.Seed);
      Assert.True(originalMsg.YourIndex == restoredMsg.YourIndex);
      Assert.True(originalMsg.TicksPerSecond == restoredMsg.TicksPerSecond);

      Assert.True(originalMsg.Players.Length == restoredMsg.Players.Length);
      for (var i = 0; i < originalMsg.Players.Length; i++)
        Assert.True(originalMsg.Players[i] == restoredMsg.Players[i]);
    }

    [Theory]
    [InlineData(uint.MaxValue / 17, ulong.MaxValue / 37, 0)]
    [InlineData(uint.MaxValue / 7, ulong.MaxValue / 42, 1)]
    public void ShouldSerializeAndDeserializeInputs(uint frame, ulong inputMask, byte playerIndex) {
      var originalMsg = new ServerMsgInput(frame, inputMask, playerIndex);

      var buf = CreateBuffer(originalMsg);
      var written = originalMsg.WriteTo(buf, 0);
      Assert.True(written == buf.Length);

      var restoredMsg = new ServerMsgInput(buf, 0);

      Assert.True(originalMsg.Frame == restoredMsg.Frame);
      Assert.True(originalMsg.InputMask == restoredMsg.InputMask);
      Assert.True(originalMsg.PlayerIndex == restoredMsg.PlayerIndex);
    }

    [Theory]
    [InlineData(new uint[] {1, 1, 4, 1}, new [] {0, 1, 2, 3})]
    [InlineData(new uint[] {7, 3, 3, 7}, new [] {4, 5, 6, 7})]
    public void ShouldSerializeAndDeserializeFinish(uint[] frames, int[] hashes) {
      var originalMsg = new ServerMsgFinish(frames, hashes);

      var buf = CreateBuffer(originalMsg);
      var written = originalMsg.WriteTo(buf, 0);
      Assert.True(written == buf.Length);

      var restoredMsg = new ServerMsgFinish(buf, 0);

      Assert.True(originalMsg.Frames.Length == restoredMsg.Frames.Length);
      for (var i = 0; i < originalMsg.Frames.Length; i++)
        Assert.True(originalMsg.Frames[i] == restoredMsg.Frames[i]);

      Assert.True(originalMsg.Hashes.Length == restoredMsg.Hashes.Length);
      for (var i = 0; i < originalMsg.Hashes.Length; i++)
        Assert.True(originalMsg.Hashes[i] == restoredMsg.Hashes[i]);
    }
  }
}