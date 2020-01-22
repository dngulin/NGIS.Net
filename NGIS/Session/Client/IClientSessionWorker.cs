using System.Collections.Generic;
using NGIS.Message.Client;
using NGIS.Message.Server;

namespace NGIS.Session.Client {
  public interface IClientSessionWorker {
    void ConnectionFailed();
    void JoiningToSession();

    void JoinedToSession();
    void SessionStarted(ServerMsgStart msgStart);

    void InputReceived(ServerMsgInput msgInput);
    (Queue<ClientMsgInputs>, ClientMsgFinished?) Process();

    void SessionFinished(ServerMsgFinish msgFinish);
    void SessionClosedWithError(ClientSessionError errorId, ServerErrorId? serverErrorId = null);
  }
}