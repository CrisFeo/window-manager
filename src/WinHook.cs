using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WinCtl {

static class WinHook {

  // Enums
  ///////////////////////

  public enum Event {
    Focus,
    Move,
  }

  enum EventType {
    SYSTEM_FOREGROUND = 0x0003,
    OBJECT_LOCATIONCHANGE = 0x800B,
  }

  [Flags]
  enum Flags {
    OUT_OF_CONTEXT = 0x0000,
    SKIP_OWN_PROCESS = 0x0002,
  }

  // Handlers
  ///////////////////////

  delegate void HookFunc(
    IntPtr hookHandle,
    EventType eventType,
    IntPtr windowHandle,
    int objectId,
    int childId,
    uint threadId,
    uint time
  );

  // DLL imports
  ///////////////////////

  [DllImport("user32.dll")]
  static extern IntPtr SetWinEventHook(
    EventType eventMin,
    EventType eventMax,
    IntPtr mod,
    HookFunc cbk,
    uint pid,
    uint thread,
    Flags flags
  );

  [DllImport("user32.dll")]
  static extern IntPtr UnhookWinEvent(IntPtr hdl);

  // Internal vars
  ///////////////////////

  static List<IntPtr> hookHandles = new List<IntPtr>();
  static HookFunc hookDelegate;
  static Action<Event, Window.Info> onEvent;

  // Public methods
  ///////////////////////

  public static bool Install(Action<Event, Window.Info> onEvent) {
    if (hookHandles.Count != 0) return false;
    WinHook.onEvent = onEvent;
    WinHook.hookDelegate = OnHook;
    EventLoop.Invoke("win hook", () => {
      AddHook(EventType.SYSTEM_FOREGROUND);
      AddHook(EventType.OBJECT_LOCATIONCHANGE);
    });
    return true;
  }

  public static bool Uninstall() {
    if (hookHandles.Count == 0) return false;
    foreach (var h in hookHandles) UnhookWinEvent(h);
    hookHandles.Clear();
    return true;
  }

  // Internal methods
  ///////////////////////

  static void OnHook(
    IntPtr hookHandle,
    EventType eventType,
    IntPtr windowHandle,
    int objectId,
    int childId,
    uint threadId,
    uint time
  ) {
    using (Instrument.GreaterThan("win hook", 150, Log.Level.Error))
    using (Instrument.GreaterThan("win hook", 50,  Log.Level.Warn)) {
      Event e;
      switch (eventType) {
        case EventType.SYSTEM_FOREGROUND:     e = Event.Focus; break;
        case EventType.OBJECT_LOCATIONCHANGE: e = Event.Move;  break;
        default: return;
      }
      if (windowHandle == IntPtr.Zero || objectId != 0) return;
      var w = Window.FromHandle(windowHandle);
      if (!w.isValid || !w.isDisplayable) return;
      if (onEvent != null) onEvent(e, w);
    }
  }

  static void AddHook(EventType eventType) {
    hookHandles.Add(SetWinEventHook(
      eventType,
      eventType,
      Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]),
      WinHook.hookDelegate,
      0,
      0,
      Flags.OUT_OF_CONTEXT | Flags.SKIP_OWN_PROCESS
    ));
  }

}

}
