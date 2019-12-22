using System;
using NGIS.Logging;

namespace NGIS.Server {
  public class ConsoleLogger : ILogger {
    public void Info(string msg) => Console.WriteLine($"{DateTime.Now} [INFO] {msg}");
    public void Warning(string msg) => Console.WriteLine($"{DateTime.Now} [WARNING] {msg}");
    public void Error(string msg) => Console.WriteLine($"{DateTime.Now} [ERROR] {msg}");

    public void Exception(Exception e) {
      Console.WriteLine($"{DateTime.Now} [EXCEPTION]");
      Console.WriteLine(e);
    }
  }
}