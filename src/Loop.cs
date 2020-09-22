using System;
using System.Collections.Concurrent;

namespace WinCtl {

static class Loop {

  // Structs
  ///////////////////////

  struct Invocation {
    public string name;
    public Action action;
    public int threadId;
    public float created;
    public float delay;
  }

  // Internal vars
  ///////////////////////

  static BlockingCollection<Invocation> inputQueue = new BlockingCollection<Invocation>();
  static PriorityQueue<Invocation> delayQueue  = new PriorityQueue<Invocation>();
  static Thread.Basic thread;
  static bool isRunning;

  // Public methods
  ///////////////////////

  public static void Start(Action initialize) {
    if (thread != null) return;
    isRunning = true;
    thread = Thread.Run("loop", () => {
      initialize();
      MainLoop();
    });
    thread.Join();
    thread = null;
  }

  public static void Stop() {
    isRunning = false;
  }

  public static void Invoke(string name, Action action) {
    InvokeAfter(name, 0, action);
  }

  public static void InvokeAfter(string name, float delay, Action action) {
    if (!isRunning) {
      Log.Error($"could not queue action with name {name}, loop is shutting down");
      return;
    }
    if (action == null) {
      Log.Error($"action was null for invocation with name {name}");
      return;
    }
    inputQueue.Add(new Invocation{
      name = name,
      action = action,
      threadId = Thread.CurrentThreadId,
      created = Time.Now(),
      delay = delay,
    });
  }

  // Internal methods
  ///////////////////////

  static void MainLoop() {
    while (isRunning) {
      InsertIntoDelayQueue(inputQueue.Take());
      var lastAdjustment = Time.Now();
      while (!delayQueue.IsEmpty()) {
        delayQueue.Adjust(Time.Now() - lastAdjustment);
        lastAdjustment = Time.Now();
        var (_, delay) = delayQueue.Peek();
        if (delay <= 0) {
          var (invocation, _) = delayQueue.Pop();
          RunInvocation(invocation);
        } else {
          if (inputQueue.TryTake(out var invocation, (int)(delay * 1000))) {
            InsertIntoDelayQueue(invocation);
          }
        }
      }
      delayQueue.Clear();
    }
  }

  static void InsertIntoDelayQueue(Invocation invocation) {
    var delay = invocation.delay;
    delay -= Time.Now() - invocation.created;
    delayQueue.Push(invocation, delay);
  }

  static void RunInvocation(Invocation invocation) {
    try {
      Log.Trace($"running invocation {invocation.name} [{invocation.threadId}] after {Time.Now() - invocation.created:F2}s (wanted {invocation.delay:F2}s)");
      invocation.action();
    } catch (Exception e) {
      Log.Error($"exception encountered for invocation with name {invocation.name}\n{e.ToString()}");
    }
  }

}

}
