namespace NGIS.Session.Server {
  public class ServerConfig {
    public string Host { get; set; }
    public int Port { get; set; }

    public string Game { get; set; }
    public ushort Version { get; set; }

    public int MaxSessions { get; set; }
    public byte SessionPlayers { get; set; }
    public byte TickPerSecond { get; set; }
  }
}