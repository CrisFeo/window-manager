using System;
using System.IO;

namespace WinCtl {

static class Program {

  [STAThread]
  static void Main(string[] args) {
    Console.WriteLine("starting up");
    if (args.Length != 1) {
      Console.WriteLine("no script file path provided");
      return;
    }
    if (!File.Exists(args[0])) {
      Console.WriteLine($"script file does not exist: {args[0]}");
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
      Console.WriteLine($"compilation error:\n{String.Join("\n", errors)}");
      return;
    }
    var isRunning = Script.Execute(assembly);
    if (!isRunning) return;
    Hotkey.MapDown(Hotkey.Mod.Win, Key.Q, false, Loop.Exit);
    Console.WriteLine($"'{args[0]}' loaded");
    Loop.Run();
    Console.WriteLine("shutting down");
    Environment.Exit(0);
  }

}

}
