using System;
using System.Runtime.InteropServices;

static class Loop {

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

  // Public methods
  ///////////////////////

  public static void Wait() {
    int ret = 0;
    while ((ret = GetMessage(out Msg msg, IntPtr.Zero, 0, 0)) != 0) {
      if (ret == -1) break;
    }
  }

}
