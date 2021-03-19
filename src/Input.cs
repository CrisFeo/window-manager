using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace WinCtl {

public static class Input {

  // Constants
  ///////////////////////

  static readonly Key[] MODIFIERS = new[] {
    Key.LeftMenu,
    Key.RightMenu,
    Key.LeftControl,
    Key.RightControl,
    Key.LeftShift,
    Key.RightShift,
    Key.LeftWindows,
    Key.RightWindows,
  };

  static readonly HashSet<Key> MASKED_MODIFIERS = new HashSet<Key> {
    Key.LeftMenu,
    Key.RightMenu,
    Key.LeftWindows,
    Key.RightWindows,
  };

  const Key MASK_KEY = Key.Unassigned;

  // Enums
  ///////////////////////

  enum Type : int {
    Mouse = 0,
    Key = 1,
    Hardware = 2,
  }

  enum KeyFlags : int {
    ExtendedKey = 0x0001,
    KeyUp = 0x0002,
    ScanCode = 0x0008,
    UniCode = 0x0004,
  }

  // Structs
  ///////////////////////

  [StructLayout(LayoutKind.Sequential)]
  struct InputMsg {
    public Type type;
    public InputData data;
    public static int Size {
      get => Marshal.SizeOf(typeof(InputMsg));
    }
  }

  [StructLayout(LayoutKind.Explicit)]
  struct InputData {
    [FieldOffset(0)] public Mouse mouse;
    [FieldOffset(0)] public Keyboard keyboard;
    [FieldOffset(0)] public Hardware hardware;
  }

  [StructLayout(LayoutKind.Sequential)]
  struct Mouse {
    public int dx;
    public int dy;
    public int mouseData;
    public int flags;
    public int time;
    public IntPtr extraInfo;
  }

  [StructLayout(LayoutKind.Sequential)]
  struct Keyboard {
    public short key;
    public short scan;
    public KeyFlags flags;
    public int time;
    public IntPtr extraInfo;
  }

  [StructLayout(LayoutKind.Sequential)]
  struct Hardware {
    public int msg;
    public short low;
    public short high;
  }

  // DLL imports
  ///////////////////////

  [DllImport("user32.dll")]
  static extern int SendInput(int count, InputMsg[] inputs, int size);

  [DllImport("user32.dll")]
  static extern IntPtr GetMessageExtraInfo();

  [DllImport("user32.dll")]
  static extern short MapVirtualKey(short code, int mapType);

  [DllImport("User32.dll")]
  static extern ushort GetAsyncKeyState(Key key);

  // Public methods
  ///////////////////////

  public static void Send(LinkedList<(Key, bool)> keystrokes) {
    foreach (var modifier in MODIFIERS) {
      if (!Hotkey.IsDown(modifier)) continue;
      keystrokes.AddFirst((MASK_KEY, false));
      keystrokes.AddFirst((modifier, false));
      keystrokes.AddFirst((MASK_KEY, true));
      keystrokes.AddLast((modifier, true));
    }
    SendInput(keystrokes);
  }

  public static void SendRaw(LinkedList<(Key, bool)> keystrokes) {
    SendInput(keystrokes);
  }

  // Internal methods
  ///////////////////////

  static void SendInput(LinkedList<(Key, bool)> keystrokes) {
    keystrokes.AddFirst(Hotkey.DISABLE_KEYSTROKE);
    keystrokes.AddLast(Hotkey.DISABLE_KEYSTROKE);
    var messages = keystrokes.Select(FromKeystroke).ToArray();
    SendInput(messages.Length, messages, InputMsg.Size);
  }

  static InputMsg FromKeystroke((Key, bool) keystroke) {
    var (key, isDown) = keystroke;
    return new InputMsg {
      type = Type.Key,
      data = new InputData {
        keyboard = new Keyboard {
          key = (short)key,
          scan = (short)(MapVirtualKey((short)key, 0) & 0xFFU),
          flags = 0
            | (isDown ? 0 : KeyFlags.KeyUp)
            | (IsExtended(key) ? KeyFlags.ExtendedKey : 0),
          time = 0,
          extraInfo = GetMessageExtraInfo(),
        },
      },
    };
  }

  static bool IsExtended(Key key) {
    if (key == Key.Menu)         return true;
    if (key == Key.LeftMenu)     return true;
    if (key == Key.RightMenu)    return true;
    if (key == Key.Control)      return true;
    if (key == Key.RightControl) return true;
    if (key == Key.Insert)       return true;
    if (key == Key.Delete)       return true;
    if (key == Key.Home)         return true;
    if (key == Key.End)          return true;
    if (key == Key.Prior)        return true;
    if (key == Key.Next)         return true;
    if (key == Key.Right)        return true;
    if (key == Key.Up)           return true;
    if (key == Key.Left)         return true;
    if (key == Key.Down)         return true;
    if (key == Key.NumLock)      return true;
    if (key == Key.Cancel)       return true;
    if (key == Key.Snapshot)     return true;
    if (key == Key.Divide)       return true;
    return false;
  }

}

}
