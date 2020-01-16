using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

static class Input {

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
    public Key key;
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

  [DllImport("User32.dll")]
  static extern ushort GetAsyncKeyState(Key key);

  // Public methods
  ///////////////////////

  public static void Send((Key, bool)[] keystrokes) {
    var (initial, reset) = ClearModifiers();
    initial.AddRange(keystrokes.Select(FromKeystroke));
    initial.AddRange(reset);
    var messages = initial.ToArray();
    SendInput(messages.Length, messages, InputMsg.Size);
  }

  // Internal methods
  ///////////////////////

  static (List<InputMsg>, List<InputMsg>) ClearModifiers() {
    var initial = new List<InputMsg>();
    var reset = new List<InputMsg>();
    initial.Add(FromKeystroke(Key.Undefined, true));
    reset.Add(FromKeystroke(Key.Undefined, true));
    foreach (var modifier in MODIFIERS) {
      if (!IsDown(modifier)) continue;
      initial.Add(FromKeystroke(modifier, false));
      reset.Add(FromKeystroke(modifier, true));
    }
    initial.Add(FromKeystroke(Key.Undefined, false));
    reset.Add(FromKeystroke(Key.Undefined, false));
    return (initial, reset);
  }

  static bool IsDown(Key key) {
    return GetAsyncKeyState(key) >> 15 == 1;
  }

  static InputMsg FromKeystroke((Key, bool) keystroke) {
    var (key, isDown) = keystroke;
    return FromKeystroke(key, isDown);
  }

  static InputMsg FromKeystroke(Key key, bool isDown) {
    return new InputMsg {
      type = Type.Key,
      data = new InputData {
        keyboard = new Keyboard {
          key = key,
          scan = 0,
          flags = isDown ? 0 : KeyFlags.KeyUp,
          time = 0,
          extraInfo = IntPtr.Zero,
        },
      },
    };
  }

}

