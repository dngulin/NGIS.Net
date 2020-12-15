using System;

namespace NGIS.Logging {
  public interface IClientSessionLogger {
    void Connecting(string host, int port);

    void Joining(string playerName, string game, ushort version);
    void Joined();

    void GameStarted();
    void GameFinished();

    void ConnectionLost();
    void SessionClosed();

    void FailedToProcessSession(Exception exception);
    void FailedToSendMessages(Exception exception);
  }
}