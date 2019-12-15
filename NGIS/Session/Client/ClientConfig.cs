namespace NGIS.Session.Client {
  public class ClientConfig {
    public readonly string Game;
    public readonly ushort Version;
    public readonly byte MaxPlayers;

    public readonly string Host;
    public readonly int Port;

    public readonly string PlayerName;

    public ClientConfig(string game, ushort version, byte maxPlayers, string host, int port, string playerName) {
      Game = game;
      Version = version;
      MaxPlayers = maxPlayers;

      Host = host;
      Port = port;

      PlayerName = playerName;
    }
  }
}