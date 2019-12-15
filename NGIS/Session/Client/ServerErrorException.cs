using System;
using NGIS.Message.Server;

namespace NGIS.Session.Client {
  public class ServerErrorException : Exception {
    public readonly ServerErrorId Error;

    public ServerErrorException(ServerErrorId error) {
      Error = error;
    }
  }
}