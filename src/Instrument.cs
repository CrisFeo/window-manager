using System;
using System.Collections.Generic;

namespace WinCtl {

static class Instrument {

  // Classes
  ///////////////////////

  public class Instance : IDisposable {

    float start;
    Action<float> action;

    public Instance(Action<float> action) {
      start = Time.Now();
      this.action = action;
    }

    public void Dispose() {
      action(Time.Now() - start);
    }

  }

  // Public methods
  ///////////////////////

  public static Instance GreaterThan(string name, long threshold, Log.Level level) {
    return new Instance(duration => {
      if (duration < threshold / 1000f) return;
      Log.At(level, $"{name} took {duration}ms (expected <{threshold}ms)");
    });
  }

}

}

