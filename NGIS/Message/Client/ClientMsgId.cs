namespace NGIS.Message.Client {
  public enum ClientMsgId : byte {
    KeepAlive = 0,
    Join,
    Inputs,
    Finished
  }
}