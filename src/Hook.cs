using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

static class Hook {

  // Enums
  ///////////////////////

  enum HookType {
    KEYBOARD_LOW_LEVEL = 13,
  }

  enum MsgType {
    KEY_DOWN = 0x100,
    KEY_UP = 0x101,
    SYS_KEY_DOWN = 0x104,
    SYS_KEY_UP = 0x105,
  }

  // Structs
  ///////////////////////

  [StructLayout(LayoutKind.Sequential)]
  private struct KeyboardMessage {
    public int keyCode;
    public int scanCode;
    public int flags;
    public int time;
    public int extra;
  }

  // Handlers
  ///////////////////////

  delegate IntPtr HookFunc(int code, MsgType type, IntPtr msg);

  // DLL imports
  ///////////////////////

  [DllImport("user32.dll", SetLastError = true)]
  static extern IntPtr SetWindowsHookEx(HookType type, HookFunc cbk, IntPtr mod, uint thread);

  [DllImport("user32.dll", SetLastError = true)]
  static extern bool UnhookWindowsHookEx(IntPtr hdl);

  [DllImport("user32.dll")]
  static extern IntPtr CallNextHookEx(IntPtr hdl, int code, MsgType type, IntPtr msg);

  // Internal vars
  ///////////////////////

  static IntPtr hookHandle;
  static int currentId;
  static Dictionary<int, Action> handlers = new Dictionary<int, Action>();

  // Public methods
  ///////////////////////

  public static void Initialize() {
    if (hookHandle != IntPtr.Zero) return;
    var module = Marshal.GetHINSTANCE(
      Assembly.GetExecutingAssembly().GetModules()[0]
    );
    hookHandle = SetWindowsHookEx(
      HookType.KEYBOARD_LOW_LEVEL,
      OnHook,
      module,
      0
    );
  }

  public static void Shutdown() {
    if (hookHandle == IntPtr.Zero) return;
    UnhookWindowsHookEx(hookHandle);
    hookHandle = IntPtr.Zero;
  }

  public static int? Register(Action callback) {
    //if (key == 0 || mods == Mod.None) return null;
    var id = currentId++;
    //var didRegister = RegisterHotKey(IntPtr.Zero, id, mods, key);
    //if (!didRegister) return null;
    handlers[id] = callback;
    return id;
  }

  public static void Unregister(int id) {
    //var didUnregister =  UnregisterHotKey(IntPtr.Zero, id);
    //if (!didUnregister) return false;
    handlers.Remove(id);
  }

  // Internal methods
  ///////////////////////

  static IntPtr OnHook(int code, MsgType type, IntPtr msgPtr) {
    if (code < 0) {
      return CallNextHookEx(hookHandle, code, type, msgPtr);
    }
    var msg = (KeyboardMessage)Marshal.PtrToStructure(
      msgPtr,
      typeof(KeyboardMessage)
    );
    var handled = false;
    switch (type) {
      case MsgType.KEY_DOWN:
      case MsgType.SYS_KEY_DOWN:
        handled = OnDown(msg);
        break;
      case MsgType.KEY_UP:
      case MsgType.SYS_KEY_UP:
        handled = OnUp(msg);
        break;
    }
    if (handled) return IntPtr.Zero;
    return CallNextHookEx(hookHandle, code, type, msgPtr);
  }

  static bool OnDown(KeyboardMessage msg) {
    Console.WriteLine($"keydown: {(Key)msg.keyCode}");
    return true;
  }

  static bool OnUp(KeyboardMessage msg) {
    Console.WriteLine($"keyup: {(Key)msg.keyCode}");
    return true;
  }

}
