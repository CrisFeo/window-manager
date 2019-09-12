using System;
using System.Runtime.InteropServices;

static class Window {

  // Enums
  ///////////////////////

  enum SystemMetric {
    SCREEN_X = 0,
    SCREEN_Y = 1,
  }

  enum ShowStyle {
    NORMAL_NO_ACTIVATE = 4,
  }

  enum SetWindowPosFlags {
    NO_Z_ORDER = 0x0004,
    NO_ACTIVATE = 0x0010,
    NO_COPY_BITS = 0x0100,
    NO_OWNER_Z_ORDER = 0x0200,
  }

  // Structs
  ///////////////////////

  public struct Info {
    public IntPtr handle;
    public int x;
    public int y;
    public int w;
    public int h;
    public bool Valid { get { return handle != null; } }
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct Rect {
    public int l;
    public int t;
    public int r;
    public int b;
    public int x { get { return l; } }
    public int y { get { return t; } }
    public int w { get { return r - l; } }
    public int h { get { return b - t; } }
  }

  // DLL imports
  ///////////////////////

  [DllImport("user32.dll", SetLastError=true)]
  static extern bool SetProcessDPIAware();

  [DllImport("user32.dll")]
  static extern int GetSystemMetrics(SystemMetric metric);

  [DllImport("user32.dll", SetLastError = true)]
  static extern bool GetWindowRect(IntPtr wnd, out Rect rect);

  [DllImport("user32.dll")]
  static extern IntPtr GetForegroundWindow();

  [DllImport("user32.dll", SetLastError = true)]
  static extern IntPtr FindWindow(string className, string windowName);

  [DllImport("user32.dll")]
  static extern bool IsZoomed(IntPtr wnd);

  [DllImport("user32.dll")]
  static extern bool ShowWindow(IntPtr wnd, ShowStyle style);

  [DllImport("user32.dll", SetLastError=true)]
  static extern bool SetWindowPos(IntPtr wnd, IntPtr afterWnd, int x, int y, int w, int h, SetWindowPosFlags flags);

  // Public methods
  ///////////////////////

  public static bool Initialize() {
    return SetProcessDPIAware();
  }

  public static (int, int) Resolution() {
    return (
      GetSystemMetrics(SystemMetric.SCREEN_X),
      GetSystemMetrics(SystemMetric.SCREEN_Y)
    );
  }

  public static Info Active() {
    return Fetch(GetForegroundWindow());
  }

  public static Info ByName(string name) {
    return Fetch(FindWindow(default, name));
  }

  public static bool Move(Info info, int? x, int? y, int? w, int? h) {
    if (!ShowWindow(info.handle, ShowStyle.NORMAL_NO_ACTIVATE)) return false;
    var xp = (x - 8  ?? info.x);
    var yp = (y      ?? info.y);
    var wp = (w + 16 ?? info.w);
    var hp = (h + 8  ?? info.h);
    var flags = SetWindowPosFlags.NO_Z_ORDER
      | SetWindowPosFlags.NO_ACTIVATE
      | SetWindowPosFlags.NO_COPY_BITS
      | SetWindowPosFlags.NO_OWNER_Z_ORDER;
    return SetWindowPos(info.handle, IntPtr.Zero, xp, yp, wp, hp, flags);
  }

  // Internal methods
  ///////////////////////

  static Info Fetch(IntPtr handle) {
    if (handle == null) return default;
    if (IsZoomed(handle)) {
      if (!ShowWindow(handle, ShowStyle.NORMAL_NO_ACTIVATE)) return default;
    }
    Rect r;
    if (!GetWindowRect(handle, out r)) return default;
    return new Info {
      handle = handle,
      x = r.x,
      y = r.y,
      w = r.w,
      h = r.h,
    };
  }

}
