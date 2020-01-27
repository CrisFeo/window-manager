using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WinCtl {

public static class Time {

  // Internal vars
  ///////////////////////

  static Stopwatch stopwatch;

  // Static constructor
  ///////////////////////

  static Time() {
    stopwatch = new Stopwatch();
    stopwatch.Start();
  }

  // Public methods
  ///////////////////////

  public static long Now() {
    return stopwatch.ElapsedMilliseconds;
  }

  public static void After(long duration, Action action) {
    Task.Run(async () => {
      await Task.Delay((int)duration);
      if (action != null) action();
    });
  }

}

}
