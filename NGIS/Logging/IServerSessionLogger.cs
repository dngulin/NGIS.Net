using System;
using NGIS.Message.Server;

namespace NGIS.Logging {
  public interface IServerSessionLogger {
    void SessionCreated(string sessionId);
    void ClientJoined(string sessionId, string clientId, string nickName);

    void SessionStarted(string sessionId);
    void SessionFinished(string sessionId);

    void SendingFinish(string sessionId);
    void FinishMessageSent(string sessionId, string clientId, string nickName, uint frame, int hash);

    void SessionClosedWithError(string sessionId, ServerErrorId errorId, Exception exception);
    void SessionClosedWithConnectionError(string sessionId);

    void ClintRemovedByTimeout(string sessionId, string clientId, string nickName);
    void ClientRemovedByProtocolError(string sessionId, string clientId, string nickName);

    void SessionClosed(string sessionId);
  }
}