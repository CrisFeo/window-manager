using System;
using System.Collections.Concurrent;

namespace WinCtl {

static class Loop {

  // Internal vars
  ///////////////////////

  static BlockingCollection<Action> queue = new BlockingCollection<Action>();
  static bool isRunning;

  // Public methods
  ///////////////////////

  public static void Run() {
    if (isRunning) return;
    isRunning = true;
    while (isRunning) {
      var action = queue.Take();
      if (action != null) action();
    }
    Environment.Exit(0);
  }

  public static bool Invoke(Action action) {
    if (!isRunning) return false;
    queue.Add(action);
    return true;
  }

  public static void Exit() {
    isRunning = false;
  }

}

}
