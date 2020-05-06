using System;
using System.Collections.Generic;

namespace WinCtl {

public static class Event {

  // Events
  ///////////////////////

  public static event Action<Window.Info> onFocus;
  public static event Action<Window.Info> onMove;

  // Static constructor
  ///////////////////////

  static Event() {
    WinHook.Install((e, i) => Loop.Invoke($"event-hook {e}", () => OnEvent(e, i)));
  }

  // Internal methods
  ///////////////////////

  static void OnEvent(WinHook.Event e, Window.Info info) {
    switch (e) {
      case WinHook.Event.Focus:
        onFocus(info);
        break;
      case WinHook.Event.Move: onMove(info); break;
    }
  }

}

}
