using System;
using System.Collections.Concurrent;

namespace WinCtl {

static class Loop {

  // Structs
  ///////////////////////

  struct Invocation {
    public string name;
    public Action action;
  }

  // Internal vars
  ///////////////////////

  static BlockingCollection<Invocation> queue = new BlockingCollection<Invocation>();
  static bool isRunning;

  // Public methods
  ///////////////////////

  public static void Run() {
    if (isRunning) return;
    isRunning = true;
    while (isRunning) {
      var invocation = queue.Take();
      if (invocation.action == null) {
        Console.WriteLine($"invoke {invocation.name}: action was null");
      } else {
        try {
          invocation.action();
        } catch (Exception e) {
          Console.WriteLine($"invoke {invocation.name}: exception\n{e.ToString()}");
        }
      }
    }
  }

  public static bool Invoke(string name, Action action) {
    if (!isRunning) return false;
    queue.Add(new Invocation{
      name = name,
      action = action,
    });
    return true;
  }

  public static void Exit() {
    isRunning = false;
  }

}

}
