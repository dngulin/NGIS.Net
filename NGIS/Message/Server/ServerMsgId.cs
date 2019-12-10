namespace NGIS.Message.Server {
  public enum ServerMsgId : byte {
    KeepAlive = 0,
    Error,
    Joined,
    Start,
    Inputs,
    Finish
  }
}