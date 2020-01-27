using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WinCtl {

public static class Window {

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
    EXTENDED_FRAME_BOUNDS = 9,
    CLOAKED = 14,
  }

  // Handlers
  ///////////////////////

  delegate bool EnumWindowsFunc(IntPtr wnd, IntPtr param);

  // Structs
  ///////////////////////

  public struct Info {
    public IntPtr handle;
    public int pid;
    public bool isVisible;
    public Rect offset;
    public int x;
    public int y;
    public int w;
    public int h;
    public bool isValid { get => handle != null; }
    public bool isDisplayable { get => w != 0 && h != 0; }
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
    public Rect(int x, int y, int w, int h) {
      l = x;
      t = y;
      r = w + l;
      b = h + t;
    }
  }

  // DLL imports
  ///////////////////////

  [DllImport("user32.dll")]
  static extern bool IsWindow(IntPtr wnd);

  [DllImport("user32.dll")]
  static extern int GetSystemMetrics(SystemMetric metric);

  [DllImport("user32.dll", SetLastError = true)]
  static extern bool GetWindowRect(IntPtr wnd, out Rect rect);

  [DllImport("user32.dll", SetLastError = true)]
  static extern bool GetClientRect(IntPtr wnd, out Rect rect);

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

  [DllImport("user32.dll", SetLastError=true)]
  static extern int GetWindowThreadProcessId(IntPtr wnd, out int pid);

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

  [DllImport("dwmapi.dll")]
  static extern int DwmGetWindowAttribute(IntPtr wnd, DwmWindowAttribute attr, out Rect val, int size);

  // Public methods
  ///////////////////////

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
    var pid = Process.GetCurrentProcess().Id;
    EnumWindows((h, p) => {
      var i = FromHandle(h);
      if (!i.isValid || i.pid == pid) return true;
      l.Add(i);
      return true;
    }, IntPtr.Zero);
    return l;
  }

  public static Info ByName(string name) {
    return FromHandle(FindWindow(default, name));
  }

  public static Info ByClass(string className) {
    return FromHandle(FindWindow(className, default));
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
    var xp = (x ?? info.x)+ info.offset.x;
    var yp = (y ?? info.y)+ info.offset.y;
    var wp = (w ?? info.w)+ info.offset.w;
    var hp = (h ?? info.h)+ info.offset.h;
    var flags = SetWindowPosFlags.NO_Z_ORDER
      | SetWindowPosFlags.NO_ACTIVATE
      | SetWindowPosFlags.NO_COPY_BITS
      | SetWindowPosFlags.NO_OWNER_Z_ORDER;
    return SetWindowPos(info.handle, IntPtr.Zero, xp, yp, wp, hp, flags);
  }

  // Internal methods
  ///////////////////////

  internal static Info FromHandle(IntPtr handle) {
    if (handle == null) return default;
    if (!IsWindow(handle)) return default;
    if (IsZoomed(handle)) {
      if (!ShowWindow(handle, ShowStyle.NORMAL_NO_ACTIVATE)) return default;
    }
    Rect rect;
    if (!GetWindowRect(handle, out rect)) return default;
    Rect extendedRect;
    if (DwmGetWindowAttribute(
      handle,
      DwmWindowAttribute.EXTENDED_FRAME_BOUNDS,
      out extendedRect,
      Marshal.SizeOf(typeof(Rect))
    ) != 0) return default;
    GetWindowThreadProcessId(handle, out int pid);
    return new Info {
      handle = handle,
      pid = pid,
      isVisible = IsVisible(handle),
      offset = new Rect(
        rect.x - extendedRect.x,
        rect.y - extendedRect.y,
        rect.w - extendedRect.w,
        rect.h - extendedRect.h
      ),
      x = extendedRect.x,
      y = extendedRect.y,
      w = extendedRect.w,
      h = extendedRect.h,
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

}
