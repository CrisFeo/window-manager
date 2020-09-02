using System;
using System.Collections.Generic;
using System.Threading;

namespace WinCtl {

public static class Instrument {

  // Classes
  ///////////////////////

  public class Instance : IDisposable {

    long start;
    Action<long> action;

    public Instance(Action<long> action) {
      start = Time.Now();
      this.action = action;
    }

    public void Dispose() {
      action(Time.Now() - start);
    }

  }

  // Public methods
  ///////////////////////

  public static Instance GreaterThan(long threshold, string name) {
    return new Instance(duration => {
      if (duration < threshold) return;
      Log.Error($"{name} took {duration}ms (expected <{threshold}ms)");
    });
  }

}

}

