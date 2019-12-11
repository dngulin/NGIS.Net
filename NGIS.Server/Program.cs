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

      using (var sessionManager = new ServerSessionManager(serverConfig)) {
        var running = true;
        Console.CancelKeyPress += (sender, eventArgs) => {
          eventArgs.Cancel = true;
          running = false;
        };

        Console.WriteLine($"Running server at {serverConfig.Host}:{serverConfig.Port}...");
        while (running)
          sessionManager.Porcess();
        Console.WriteLine("Stopping server...");
      }

      return 0;
    }
  }
}