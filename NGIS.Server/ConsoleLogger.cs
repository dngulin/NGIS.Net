using System;
using NGIS.Logging;

namespace NGIS.Server {
  public class ConsoleLogger : ILogger {
    public void Info(string msg) => Console.WriteLine($"[INFO] {msg}");
    public void Warning(string msg) => Console.WriteLine($"[WARNING] {msg}");
    public void Error(string msg) => Console.WriteLine($"[ERROR] {msg}");

    public void Exception(Exception e) {
      Console.WriteLine("[EXCEPTION]");
      Console.WriteLine(e);
    }
  }
}