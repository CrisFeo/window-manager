using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WinCtl {

static class KeyHook {

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
  struct KeyboardMessage {
    public int keyCode;
    public int scanCode;
    public int flags;
    public int time;
    public int extra;
  }

  // Handlers
  ///////////////////////

  delegate IntPtr HookFunc(int code, IntPtr type, IntPtr msg);

  // DLL imports
  ///////////////////////

  [DllImport("user32.dll")]
  static extern IntPtr SetWindowsHookEx(HookType type, HookFunc cbk, IntPtr mod, int thread);

  [DllImport("user32.dll")]
  static extern IntPtr CallNextHookEx(IntPtr hdl, int code, IntPtr type, IntPtr msg);

  [DllImport("user32.dll")]
  static extern bool UnhookWindowsHookEx(IntPtr hdl);

  // Internal vars
  ///////////////////////

  static IntPtr hookHandle;
  static HookFunc hookDelegate;
  static Func<Key, bool> onDown;
  static Func<Key, bool> onUp;

  // Public methods
  ///////////////////////

  public static bool Install(
    Func<Key, bool> onDown,
    Func<Key, bool> onUp
  ) {
    if (hookHandle != IntPtr.Zero) return false;
    KeyHook.onDown = onDown;
    KeyHook.onUp = onUp;
    KeyHook.hookDelegate = OnHook;
    hookHandle = SetWindowsHookEx(
      HookType.KEYBOARD_LOW_LEVEL,
      KeyHook.hookDelegate,
      Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]),
      0
    );
    return true;
  }

  public static bool Uninstall() {
    if (hookHandle == IntPtr.Zero) return false;
    UnhookWindowsHookEx(hookHandle);
    hookHandle = IntPtr.Zero;
    return true;
  }

  // Internal methods
  ///////////////////////

  static IntPtr OnHook(int code, IntPtr typePtr, IntPtr msgPtr) {
    if (code < 0) {
      return CallNextHookEx(hookHandle, code, typePtr, msgPtr);
    }
    var msg = (KeyboardMessage)Marshal.PtrToStructure(
      msgPtr,
      typeof(KeyboardMessage)
    );
    var handled = false;
    switch ((MsgType)typePtr.ToInt32()) {
      case MsgType.KEY_DOWN:
      case MsgType.SYS_KEY_DOWN:
        handled = onDown((Key)msg.keyCode);
        break;
      case MsgType.KEY_UP:
      case MsgType.SYS_KEY_UP:
        handled = onUp((Key)msg.keyCode);
        break;
    }
    if (handled) return new IntPtr(-1);
    return CallNextHookEx(hookHandle, code, typePtr, msgPtr);
  }

}

}
