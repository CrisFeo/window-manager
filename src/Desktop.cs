using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.Win32;

namespace WinCtl {

public static class Desktop {

  // Constants
  ///////////////////////

  const string EXPLORER_PATH =
  "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer";

  // Internal vars
  ///////////////////////

    static Lock.Mutex actionLock = Lock.New();

  // Public methods
  ///////////////////////

  public static void GoTo(int target) {
    using (Lock.Acquire(actionLock)) {
      var keystrokes = new LinkedList<(Key, bool)>();
      var w = Window.ByName("Search");
      if (w.isValid && w.isVisible) {
        keystrokes.AddLast((Key.RightWindows, true));
        keystrokes.AddLast((Key.RightWindows, false));
        Input.Send(keystrokes);
        keystrokes = new LinkedList<(Key, bool)>();
      }
      var (current, total) = Fetch();
      if (target < 0 || target >= total) return;
      if (current == -1 || target == current) return;
      var steps = Math.Abs(target - current);
      var movementKey = target < current ? Key.Left : Key.Right;
      keystrokes.AddLast((Key.RightWindows, true));
      keystrokes.AddLast((Key.RightControl, true));
      for (var i = 0; i < steps; i++) {
        keystrokes.AddLast((movementKey, true));
        keystrokes.AddLast((movementKey, false));
      }
      keystrokes.AddLast((Key.RightControl, false));
      keystrokes.AddLast((Key.RightWindows, false));
      Input.SendRaw(keystrokes);
    }
  }

  // Internal methods
  ///////////////////////

  static (int, int) Fetch() {
    var sessionId = Process.GetCurrentProcess().SessionId;
    var current = (byte[])Registry.GetValue(
      $"{EXPLORER_PATH}\\SessionInfo\\{sessionId}\\VirtualDesktops",
      "CurrentVirtualDesktop",
      new byte[]{}
    );
    var idSize = current.Length;
    if (idSize == 0) {
      Log.Warn("current desktop id was empty");
      return (-1, 0);
    }
    var all = (byte[])Registry.GetValue(
      $"{EXPLORER_PATH}\\VirtualDesktops",
      "VirtualDesktopIDs",
      new byte[]{}
    );
    if (all.Length == 0) {
      Log.Warn("all desktop ids was empty");
      return (-1, 0);
    }
    var currentIndex = -1;
    var count = all.Length / idSize;
    for (var i = 0; i < count; i++) {
      if (EqualAt(current, all, i * idSize)) {
        currentIndex = i;
        break;
      }
    }
    if (currentIndex == -1) {
      Log.Warn("current desktop id could not be indexed");
      return (-1, 0);
    }
    return (currentIndex, count);
  }

  static bool EqualAt(byte[] a, byte[] b, int index) {
    for (var i = 0; i < a.Length; i++) {
      if (a[i] != b[index + i]) return false;
    }
    return true;
  }

}

}
