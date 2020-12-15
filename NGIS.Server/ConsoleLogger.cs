using System;
using NGIS.Logging;
using NGIS.Message.Server;

namespace NGIS.Server {
  public class ConsoleLogger : IServerSessionLogger {
    public void Info(string msg) => Console.WriteLine($"{DateTime.Now} [INFO] {msg}");
    public void Warning(string msg) => Console.WriteLine($"{DateTime.Now} [WARNING] {msg}");
    public void Error(string msg) => Console.WriteLine($"{DateTime.Now} [ERROR] {msg}");

    public void Exception(Exception e) {
      Console.WriteLine($"{DateTime.Now} [EXCEPTION]");
      Console.WriteLine(e);
    }

    public void SessionCreated(string sessionId) => Info($"Created session {sessionId}");

    public void ClientJoined(string sessionId, string clientId, string nickName) {
      Info($"Client {clientId} '{nickName}' joined to session {sessionId}");
    }

    public void SessionStarted(string sessionId) => Info($"Session {sessionId} started");

    public void SessionFinished(string sessionId) => Info($"Session {sessionId} finished and closed");

    public void SendingFinish(string sessionId) => Info($"Sending finish message for session {sessionId}...");

    public void FinishMessageSent(string sessionId, string clientId, string nickName, uint frame, int hash) {
      Info($"Client {clientId} '{nickName}' finished at {frame} with state hash {hash} in session {sessionId}");
    }

    public void SessionClosedWithError(string sessionId, ServerErrorId errorId, Exception exception) {
      Error($"Session {sessionId} closed with error id {errorId}");
      Exception(exception);
    }

    public void SessionClosedWithConnectionError(string sessionId) {
      Error($"Session {sessionId} closed with connection error");
    }

    public void ClintRemovedByTimeout(string sessionId, string clientId, string nickName) {
      Warning($"Remove disconnected client {clientId} '{nickName}' from session {sessionId}");
    }

    public void ClientRemovedByProtocolError(string sessionId, string clientId, string nickName) {
      Warning($"Remove client {clientId} '{nickName}' from session {sessionId} because of protocol error");
    }

    public void SessionClosed(string sessionId) => Warning($"Session {sessionId} closed externally");
  }
}