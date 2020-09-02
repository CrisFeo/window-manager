using System;
using System.Threading;
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

  public static void Run(Action initialize) {
    if (isRunning) return;
    isRunning = true;
    Invoke("initialize loop", initialize);
    var t = new Thread(() => {
      while (isRunning) {
        var invocation = queue.Take();
        if (invocation.action == null) {
          Log.Error($"action was null for invocation with name {invocation.name}");
        } else {
          try {
            Log.Trace($"running invocation with name {invocation.name}");
            invocation.action();
          } catch (Exception e) {
            Log.Error($"exception encountered for invocation with name {invocation.name}\n{e.ToString()}");
          }
        }
      }
    });
    t.SetApartmentState(ApartmentState.STA);
    t.Start();
    t.Join();
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
