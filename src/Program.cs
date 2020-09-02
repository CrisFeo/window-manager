using System;
using System.IO;

namespace WinCtl {

static class Program {

  [STAThread]
  static void Main(string[] args) {
    try {
      Log.Info("starting up");
      if (args.Length != 1) {
        Log.Error("no script file path provided");
        return;
      }
      if (!File.Exists(args[0])) {
        Log.Error($"script file does not exist: {args[0]}");
        return;
      }
      var (assembly, errors) = Script.Compile(
        File.ReadAllText(args[0]),
        new[] {
          typeof(System.Collections.Generic.HashSet<int>),
          typeof(System.Linq.Enumerable),
          typeof(WinCtl.Program)
        }
      );
      if (errors != null) {
        Log.Error($"compilation error:\n{String.Join("\n", errors)}");
        return;
      }
      Loop.Run(() => {
        var isRunning = Script.Execute(assembly);
        if (!isRunning) Loop.Exit();
        Log.Info($"'{args[0]}' loaded");
      });
      Log.Info("shutting down");
      Environment.Exit(0);
    } finally {
      Lock.Dispose();
      KeyHook.Uninstall();
      WinHook.Uninstall();
    }
  }

}

}
