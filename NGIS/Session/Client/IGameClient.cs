using System.Collections.Generic;
using NGIS.Message.Client;
using NGIS.Message.Server;

namespace NGIS.Session.Client {
  public interface IGameClient {
    void SessionStarted(ServerMsgStart msgStart);

    void InputReceived(ServerMsgInput msgInput);
    (Queue<ClientMsgInputs>, ClientMsgFinished?) Process();

    void SessionFinished(ServerMsgFinish msgFinish);

    void SessionClosedByServerError(ServerErrorId errorId);
    void SessionClosedByConnectionError();
    void SessionClosedByInternalError();
  }
}