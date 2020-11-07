using System;
using NGIS.Message.Server;

namespace NGIS.Session.Client {
  public struct ProcessingResult {
    public enum ResultType {
      None,
      Error,
      Joined,
      Started,
      Active,
      Finished
    }

    public readonly ResultType Type;

    private ClientSessionError? _error;
    private ServerMsgStart? _msgStart;
    private ServerMsgFinish? _msgFinish;

    public ClientSessionError ClientSessionError => _error ?? throw new InvalidOperationException();
    public ServerMsgStart StartMessage => _msgStart ?? throw new InvalidOperationException();
    public ServerMsgFinish FinishMessage => _msgFinish ?? throw new InvalidOperationException();

    private ServerErrorId? _serverErrorId;
    public ServerErrorId? ServerErrorId => _error.HasValue ? _serverErrorId : throw new InvalidOperationException();

    private ProcessingResult(ResultType type) {
      Type = type;
      _error = null;
      _serverErrorId = null;
      _msgStart = null;
      _msgFinish = null;
    }

    public static ProcessingResult None() {
      return new ProcessingResult(ResultType.None);
    }

    public static ProcessingResult Error(ClientSessionError error, ServerErrorId? serverErrorId = null) {
      return new ProcessingResult(ResultType.Error) {_error = error, _serverErrorId = serverErrorId};
    }

    public static ProcessingResult Joined() {
      return new ProcessingResult(ResultType.Joined);
    }

    public static ProcessingResult Started(ServerMsgStart msgStart) {
      return new ProcessingResult(ResultType.Started) {_msgStart = msgStart};
    }

    public static ProcessingResult Active() {
      return new ProcessingResult(ResultType.Active);
    }

    public static ProcessingResult Finished(ServerMsgFinish msgFinish) {
      return new ProcessingResult(ResultType.Finished) {_msgFinish = msgFinish};
    }
  }
}