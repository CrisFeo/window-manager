using System;
using System.Runtime.InteropServices;

static class Loop {

  // Constants
  ///////////////////////

  const int WM_USER = 0x0400;

  // Structs
  ///////////////////////

  [StructLayout(LayoutKind.Sequential)]
  struct Point {
    public int x;
    public int y;
  }

  [StructLayout(LayoutKind.Sequential)]
  struct Msg {
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

  [DllImport("user32.dll")]
  static extern bool PostMessage(IntPtr wnd, uint msg, IntPtr wParam, IntPtr lParam);

  // Internal vars
  ///////////////////////

  static event Action onNotify;
  static bool isExiting;

  // Public methods
  ///////////////////////

  public static void MainThread(Action action) {
    Action wrappedAction = null;
    wrappedAction = () => {
      action();
      onNotify -= wrappedAction;
    };
    onNotify += wrappedAction;
    PostMessage(IntPtr.Zero, WM_USER, IntPtr.Zero, IntPtr.Zero);
  }

  public static void Exit() {
    MainThread(() => isExiting = true);
  }

  public static void Wait() {
    int ret = 0;
    while ((ret = GetMessage(out Msg msg, IntPtr.Zero, WM_USER, WM_USER)) != 0) {
      if (ret == -1) break;
      if (onNotify != null) onNotify();
      if (isExiting) break;
    }
  }

}
