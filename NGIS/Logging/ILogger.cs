using System;

namespace NGIS.Logging {
  public interface ILogger {
    void Info(string msg);
    void Warning(string msg);
    void Error(string msg);
    void Exception(Exception e);
  }
}