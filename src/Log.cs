using System;
using System.Threading;
using System.Collections.Concurrent;

namespace WinCtl {

public static class Log {


  // Internal vars
  ///////////////////////

  static BlockingCollection<string> queue = new BlockingCollection<string>();
  static bool isRunning;

  // Static constructor
  ///////////////////////

  static Log() {
    if (isRunning) return;
    isRunning = true;
    var t = new Thread(() => {
      while (isRunning) {
        Console.WriteLine(queue.Take());
      }
    });
    t.Start();
  }

  // Public methods
  ///////////////////////

  public static void Trace(string message) {
    queue.Add($"[TRACE] [{Thread.CurrentThread.ManagedThreadId}] {message}");
  }

  public static void Info(string message) {
    queue.Add($"[INFO]  [{Thread.CurrentThread.ManagedThreadId}] {message}");
  }

  public static void Error(string message) {
    queue.Add($"[ERROR] [{Thread.CurrentThread.ManagedThreadId}] {message}");
  }

}

}
