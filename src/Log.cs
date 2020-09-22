using System;
using System.Collections.Concurrent;

namespace WinCtl {

public static class Log {

  // Enums
  ///////////////////////

  public enum Level {
    Trace,
    Info,
    Warn,
    Error,
  }

  // Structs
  ///////////////////////

  struct Entry {
    public Level level;
    public int threadId;
    public string message;
  }

  // Internal vars
  ///////////////////////

  static BlockingCollection<Entry> queue = new BlockingCollection<Entry>();
  static bool isRunning;

  // Static constructor
  ///////////////////////

  static Log() {
    if (isRunning) return;
    isRunning = true;
    var t = Thread.Run("log", () => {
      var sb = new System.Text.StringBuilder();
      while (isRunning) {
        var e = queue.Take();
        switch (e.level) {
          case Level.Trace: sb.Append("[Trace] ["); break;
          case Level.Info:  sb.Append("[Info]  ["); break;
          case Level.Warn:  sb.Append("[Warn]  ["); break;
          case Level.Error: sb.Append("[Error] ["); break;
        }
        sb.Append(e.threadId);
        sb.Append("] ");
        sb.Append(e.message);
        sb.Replace("\n", "\n  ");
        Console.WriteLine(sb.ToString());
        sb.Clear();
      }
    });
  }

  // Public methods
  ///////////////////////

  public static void At(Level level, string message) {
    queue.Add(new Entry {
      level = level,
      threadId = Thread.CurrentThreadId,
      message = message,
    });
  }

  public static void Trace(string message) {
    At(Level.Trace, message);
  }

  public static void Info(string message) {
    At(Level.Info, message);
  }

  public static void Warn(string message) {
    At(Level.Warn, message);
  }

  public static void Error(string message) {
    At(Level.Error, message);
  }

}

}
