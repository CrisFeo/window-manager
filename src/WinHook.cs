using System;
using System.Reflection;
using System.Runtime.InteropServices;

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

  static IntPtr hookHandle;
  static HookFunc hookDelegate;
  static Action<Event, Window.Info> onEvent;

  // Public methods
  ///////////////////////

  public static bool Install(Action<Event, Window.Info> onEvent) {
    if (hookHandle != IntPtr.Zero) return false;
    WinHook.onEvent = onEvent;
    WinHook.hookDelegate = OnHook;
    hookHandle = SetWinEventHook(
      EventType.SYSTEM_FOREGROUND,
      EventType.OBJECT_LOCATIONCHANGE,
      Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]),
      WinHook.hookDelegate,
      0,
      0,
      Flags.OUT_OF_CONTEXT | Flags.SKIP_OWN_PROCESS
    );
    return true;
  }

  public static bool Uninstall() {
    if (hookHandle == IntPtr.Zero) return false;
    UnhookWinEvent(hookHandle);
    hookHandle = IntPtr.Zero;
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

