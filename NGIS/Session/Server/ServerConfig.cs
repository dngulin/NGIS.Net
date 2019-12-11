namespace NGIS.Session.Server {
  public class ServerConfig {
    public string Host;
    public int Port;

    public string Game;
    public ushort Version;
    public int MaxSessions;

    public byte SessionPlayers;
    public byte TickPerSecond;
  }
}