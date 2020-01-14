using System;
using System.Collections.Generic;

static class Event {

  // Events
  ///////////////////////

  public static event Action<Window.Info> onFocus;
  public static event Action<Window.Info> onMove;

  // Static constructor
  ///////////////////////

  static Event() {
    WinHook.Install((e, i) => Loop.Invoke(() => OnEvent(e, i)));
  }

  // Internal methods
  ///////////////////////

  static void OnEvent(WinHook.Event e, Window.Info info) {
    switch (e) {
      case WinHook.Event.Focus: onFocus(info); break;
      case WinHook.Event.Move: onMove(info); break;
    }
  }

}

