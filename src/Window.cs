using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

static class Window {

  // Constants
  ///////////////////////

  static readonly string[] IGNORED_WINDOW_CLASSES = new[] {
    "Progman",
    "Shell_TrayWnd",
  };

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

  enum DwmWindowAttribute {
    CLOAKED = 14,
  }

  // Handlers
  ///////////////////////

  delegate bool EnumWindowsFunc(IntPtr wnd, IntPtr param);

  // Structs
  ///////////////////////

  public struct Info {
    public IntPtr handle;
    public bool isVisible;
    public int x;
    public int y;
    public int w;
    public int h;
    public bool Valid { get { return handle != null; } }
  }

  [StructLayout(LayoutKind.Sequential)]
  struct Rect {
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

  [DllImport("user32.dll")]
  static extern bool SetForegroundWindow(IntPtr wnd);

  [DllImport("user32.dll")]
  static extern bool EnumWindows(EnumWindowsFunc cbk, IntPtr param);

  [DllImport("user32.dll", SetLastError = true)]
  static extern IntPtr FindWindow(string className, string windowName);

  [DllImport("user32.dll")]
  private static extern int GetWindowTextLength(IntPtr wnd);

  [DllImport("user32.dll")]
  private static extern int GetWindowText(IntPtr wnd, StringBuilder s, int maxLen);

  [DllImport("user32.dll", SetLastError = true)]
  public static extern int GetClassName(IntPtr wnd, StringBuilder s, int maxLen);

  [DllImport("user32.dll", SetLastError = true)]
  static extern bool IsWindowVisible(IntPtr wnd);

  [DllImport("user32.dll")]
  static extern bool IsZoomed(IntPtr wnd);

  [DllImport("user32.dll")]
  static extern bool ShowWindow(IntPtr wnd, ShowStyle style);

  [DllImport("user32.dll", SetLastError=true)]
  static extern bool SetWindowPos(IntPtr wnd, IntPtr afterWnd, int x, int y, int w, int h, SetWindowPosFlags flags);

  [DllImport("dwmapi.dll")]
  static extern int DwmGetWindowAttribute(IntPtr wnd, DwmWindowAttribute attr, out bool val, int size);

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
    return FromHandle(GetForegroundWindow());
  }

  public static List<Info> All() {
    var l = new List<Info>();
    EnumWindows((h, p) => {
      var i = FromHandle(h);
      if (!i.Valid) return true;
      l.Add(i);
      return true;
    }, IntPtr.Zero);
    return l;
  }

  public static Info ByName(string name) {
    return FromHandle(FindWindow(default, name));
  }

  public static string Title(Info info) {
    var l = GetWindowTextLength(info.handle);
    if (l == 0) return null;
    var b = new StringBuilder(l);
    GetWindowText(info.handle, b, b.Capacity + 1);
    return b.ToString();
  }

  public static string Class(Info info) {
    return ClassName(info.handle);
  }

  public static bool SetActive(Info info) {
    return SetForegroundWindow(info.handle);
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

  static Info FromHandle(IntPtr handle) {
    if (handle == null) return default;
    if (IsZoomed(handle)) {
      if (!ShowWindow(handle, ShowStyle.NORMAL_NO_ACTIVATE)) return default;
    }
    Rect r;
    if (!GetWindowRect(handle, out r)) return default;
    return new Info {
      handle = handle,
      isVisible = IsVisible(handle),
      x = r.x,
      y = r.y,
      w = r.w,
      h = r.h,
    };
  }

  static bool IsVisible(IntPtr handle) {
    if (!IsWindowVisible(handle)) return false;
    var result = DwmGetWindowAttribute(
      handle,
      DwmWindowAttribute.CLOAKED,
      out bool isCloaked,
      Marshal.SizeOf(typeof(bool))
    );
    if (result != 0 || isCloaked) return false;
    return !IGNORED_WINDOW_CLASSES.Contains(ClassName(handle));
  }

  static string ClassName(IntPtr handle) {
    var b = new StringBuilder(1024);
    GetClassName(handle, b, b.Capacity + 1);
    return b.ToString();
  }

}
