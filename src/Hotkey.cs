using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

static class Hotkey {

  // Constants
  ///////////////////////

  const int MSG_HOTKEY = 0x0312;

  // Enums
  ///////////////////////

  [Flags]
  public enum Mod {
    None = 0,
    Alt = 1,
    Ctrl = 2,
    Shift = 4,
    Win = 8
  }

  // Structs
  ///////////////////////

  [StructLayout(LayoutKind.Sequential)]
  public struct Point
  {
    public int x;
    public int y;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct Msg {
    public IntPtr wnd;
    public uint message;
    public UIntPtr wParam;
    public IntPtr lParam;
    public int time;
    public Point pt;
  }

  // DLL imports
  ///////////////////////

  [DllImport("user32.dll")]
  static extern int GetMessage(out Msg msg, IntPtr wnd, uint min, uint max);

  [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
  static extern bool RegisterHotKey(IntPtr wnd, int id, Mod mods, Key key);

  [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
  private static extern bool UnregisterHotKey(IntPtr wnd, int id);

  // Internal vars
  ///////////////////////

  static int currentId;
  static Dictionary<int, Func<bool>> handlers = new Dictionary<int, Func<bool>>();

  // Public methods
  ///////////////////////

  public static int? Register(Mod mods, Key key, Action callback) {
    return Register(mods, key, () => { callback(); return true; });
  }

  public static int? Register(Mod mods, Key key, Func<bool> callback) {
    if (key == 0 || mods == Mod.None) return null;
    var id = currentId++;
    var didRegister = RegisterHotKey(IntPtr.Zero, id, mods, key);
    if (!didRegister) return null;
    handlers[id] = callback;
    return id;
  }

  public static bool Unregister(int id) {
    var didUnregister =  UnregisterHotKey(IntPtr.Zero, id);
    if (!didUnregister) return false;
    handlers.Remove(id);
    return true;
  }

  public static void Listen() {
    Func<bool> handler;
    var msg = new Msg();
    var ret = 0;
    while ((ret = GetMessage(out msg, IntPtr.Zero, 0, 0)) != 0) {
      if (ret == -1) break;
      if (msg.message == MSG_HOTKEY) {
        if (!handlers.TryGetValue((int)msg.wParam, out handler)) break;
        if (!handler.Invoke()) break;
      }
    }
  }

}
