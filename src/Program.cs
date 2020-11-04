using System;
using System.IO;

using BindingFlags = System.Reflection.BindingFlags;

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
      var (run, ok) = Compile(args[0]);
      if (ok) {
        Loop.Start(() => {
          run();
          Log.Info($"'{args[0]}' loaded");
        });
      }
      Log.Info("shutting down");
      Environment.Exit(0);
    } finally {
      Loop.Stop();
      Lock.DisposeAll();
      KeyHook.Uninstall();
      WinHook.Uninstall();
    }
  }

  static (Action, bool) Compile(string fileName) {
    var startTime = Time.Now();
    var source = File.ReadAllText(fileName);
    var asmGen = new AssemblyGenerator();
    asmGen.ReferenceAssemblyByName("System.Collections");
    asmGen.ReferenceAssemblyByName("System.Linq");
    asmGen.ReferenceAssemblyContainingType(typeof(Program));
    source = asmGen.Format(source);
    var (assembly, errors) = asmGen.Generate(source);
    if (errors != null) {
      Log.Error($"compilation error:\n{String.Join("\n", errors)}");
      return (null, false);
    }
    var scriptType = assembly.GetType("Script");
    var mainMethod = scriptType.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static);
    var run = (Action)mainMethod.CreateDelegate(typeof(Action));
    var duration = Time.Now() - startTime;
    Log.Info($"compiled script {fileName} in {duration:F2}s");
    return (run, true);
  }

}

}
