using System;
using System.IO;
using System.Text.Json;
using NGIS.Session.Server;

namespace NGIS.Server {
  internal static class Program {
    private static int Main(string[] args) {
      if (args.Length == 0) {
        Console.WriteLine("Please pass path to server-config.json as first argument");
        return 1;
      }

      var configContents = File.ReadAllText(args[0]);
      var serverConfig = JsonSerializer.Deserialize<ServerConfig>(configContents);

      var logger = new ConsoleLogger();

      using (var sessionManager = new ServerSessionManager(serverConfig, logger)) {
        var running = true;
        Console.CancelKeyPress += (sender, eventArgs) => {
          eventArgs.Cancel = true;
          running = false;
        };

        while (running)
          sessionManager.Process();
      }

      return 0;
    }
  }
}