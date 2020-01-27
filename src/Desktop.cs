using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.Win32;

namespace WinCtl {

public static class Desktop {

  // Constants
  ///////////////////////

  const string TASKBAR_CLASS = "Shell_TrayWnd";
  const string EXPLORER_PATH =
  "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer";

  // Public methods
  ///////////////////////

  public static void GoTo(int index) {
    var (current, all) = Fetch();
    if (index < 0 || index >= all.Length) return;
    var currentIndex = IndexOf(all, current);
    if (currentIndex == -1 || index == currentIndex) return;
    var movementKey = index < currentIndex ? Key.Left : Key.Right;
    var keystrokes = new LinkedList<(Key, bool)>();
    keystrokes.AddLast((Key.LeftControl, true));
    keystrokes.AddLast((Key.LeftWindows, true));
    for (var i = 0; i < Math.Abs(index - currentIndex); i++) {
      keystrokes.AddLast((movementKey, true));
      keystrokes.AddLast((movementKey, false));
    }
    keystrokes.AddLast((Key.LeftWindows, false));
    keystrokes.AddLast((Key.LeftControl, false));
    Window.SetActive(Window.ByClass(TASKBAR_CLASS));
    Input.Send(keystrokes);
  }

  // Internal methods
  ///////////////////////

  static (byte[], byte[][]) Fetch() {
    var sessionId = Process.GetCurrentProcess().SessionId;
    var current = (byte[])Registry.GetValue(
      $"{EXPLORER_PATH}\\SessionInfo\\{sessionId}\\VirtualDesktops",
      "CurrentVirtualDesktop",
      new byte[]{}
    );
    var ids = (byte[])Registry.GetValue(
      $"{EXPLORER_PATH}\\VirtualDesktops",
      "VirtualDesktopIDs",
      new byte[]{}
    );
    var size = current.Length;
    var count = ids.Length / size;
    var all = new byte[count][];
    for (var i = 0; i < count; i++) {
      all[i] = new byte[size];
      Array.Copy(ids, i * size, all[i], 0, size);
    }
    return (current, all);
  }

  static int IndexOf(byte[][] list, byte[] value) {
    for (var i = 0; i < list.Length; i++) {
      if (Equal(list[i], value)) return i;
    }
    return -1;
  }

  static bool Equal(byte[] a, byte[] b) {
    if (a.Length != b.Length) return false;
    if (object.ReferenceEquals(a,b)) return true;
    for (int i = 0; i < a.Length; i++) {
      if (a[i] != b[i]) return false;
    }
    return true;
  }

}

}
