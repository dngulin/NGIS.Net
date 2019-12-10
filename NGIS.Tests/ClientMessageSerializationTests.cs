using NGIS.Message.Client;
using NGIS.Serialization;
using Xunit;

namespace NGIS.Tests {
  public class ClientMessageSerializationTests {
    private static byte[] CreateBuffer<T>(T msg) where T : struct, IClientSerializableMsg {
      return new byte[msg.GetSerializedSize()];
    }

    [Fact]
    public void ShouldSerializeKeepAlive() {
      var msg = new ClientMsgKeepAlive();

      var buf = CreateBuffer(msg);
      var written = msg.WriteTo(buf, 0);
      Assert.True(written == buf.Length);

      var idx = 0;
      MsgSerializer.ValidateHeader(buf, (byte) ClientMsgId.KeepAlive, ref idx);
    }

    [Theory]
    [InlineData("Game Name 1", "Player 1", (ushort) 0)]
    [InlineData("Game Name 2", "Player 2", (ushort) 125)]
    public void ShouldSerializeAndDeserializeJoin(string game, string player, ushort version) {
      var originalMsg = new ClientMsgJoin(game, player, version);

      var buf = CreateBuffer(originalMsg);
      var written = originalMsg.WriteTo(buf, 0);
      Assert.True(written == buf.Length);

      var restoredMsg = new ClientMsgJoin(buf, 0);

      Assert.True(originalMsg.GameName == restoredMsg.GameName);
      Assert.True(originalMsg.PlayerName == restoredMsg.PlayerName);
      Assert.True(originalMsg.ProtocolVersion == restoredMsg.ProtocolVersion);
    }

    [Theory]
    [InlineData(0u, 25ul)]
    [InlineData(123456u, 0xFFFFFF7ul)]
    public void ShouldSerializeAndDeserializeInputs(uint frame, ulong inputMask) {
      var originalMsg = new ClientMsgInputs(frame, inputMask);

      var buf = CreateBuffer(originalMsg);
      var written = originalMsg.WriteTo(buf, 0);
      Assert.True(written == buf.Length);

      var restoredMsg = new ClientMsgInputs(buf, 0);

      Assert.True(originalMsg.Frame == restoredMsg.Frame);
      Assert.True(originalMsg.InputMask == restoredMsg.InputMask);
    }

    [Theory]
    [InlineData(1234551u, 1234599)]
    [InlineData(2345643u, -987654)]
    public void ShouldSerializeAndDeserializeFinished(uint frame, int stateHash) {
      var originalMsg = new ClientMsgFinished(frame, stateHash);

      var buf = CreateBuffer(originalMsg);
      var written = originalMsg.WriteTo(buf, 0);
      Assert.True(written == buf.Length);

      var restoredMsg = new ClientMsgFinished(buf, 0);

      Assert.True(originalMsg.Frame == restoredMsg.Frame);
      Assert.True(originalMsg.StateHash == restoredMsg.StateHash);
    }
  }
}