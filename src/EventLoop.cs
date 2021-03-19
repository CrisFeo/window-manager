using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace WinCtl {

static class EventLoop {

  // Constants
  ///////////////////////

  const string CLASS_NAME = "WinCtlMessageWindow";

  // DLL imports
  ///////////////////////

  const int WM_USER = 0x0400;

  static IntPtr HWND_MESSAGE = new IntPtr(-3);

  private delegate IntPtr WindowProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

  [StructLayout(LayoutKind.Sequential)]
  private struct WndClassEx {
    [MarshalAs(UnmanagedType.U4)]
    public int cbSize;
    [MarshalAs(UnmanagedType.U4)]
    public int style;
    public WindowProc lpfnWndProc;
    public int cbClsExtra;
    public int cbWndExtra;
    public IntPtr hInstance;
    public IntPtr hIcon;
    public IntPtr hCursor;
    public IntPtr hbrBackground;
    public string lpszMenuName;
    public string lpszClassName;
    public IntPtr hIconSm;

    public static WndClassEx New() {
      return new WndClassEx {
        cbSize = Marshal.SizeOf(typeof (WndClassEx)),
      };
    }
  }

  [DllImport("user32.dll", SetLastError = true)]
  static extern IntPtr RegisterClassExW(ref WndClassEx lpWndClass);

  [DllImport("user32.dll", SetLastError = true)]
  static extern IntPtr CreateWindowExW(
    UInt32 dwExStyle,
    IntPtr lpClassName,
    string lpWindowName,
    UInt32 dwStyle,
    Int32 x,
    Int32 y,
    Int32 nWidth,
    Int32 nHeight,
    IntPtr hWndParent,
    IntPtr hMenu,
    IntPtr hInstance,
    IntPtr lpParam
  );

  [DllImport("user32.dll", SetLastError = true)]
  static extern IntPtr DefWindowProcW(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

  [DllImport("user32.dll", SetLastError = true)]
  static extern bool DestroyWindow(IntPtr hWnd);

  [DllImport("user32.dll")]
  static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

  [DllImport("kernel32.dll")]
  public static extern IntPtr GetModuleHandle(IntPtr lpModuleName);

  // Structs
  ///////////////////////

  struct Invocation {
    public string name;
    public Action action;
    public int threadId;
  }

  // Internal vars
  ///////////////////////

  static IntPtr handle;
  static WindowProc wndProcDelegate;
  static Thread.Instance thread;
  static BlockingCollection<Invocation> invocations = new BlockingCollection<Invocation>();

  // Public methods
  ///////////////////////

  public static void Start() {
    if (thread != null) return;
    thread = Thread.Run("event loop", () => {
      var moduleHandle = GetModuleHandle(IntPtr.Zero);
      wndProcDelegate = OnWndProc;
      var wndClass = WndClassEx.New();
      wndClass.lpszClassName = CLASS_NAME;
      wndClass.lpfnWndProc = wndProcDelegate;
      wndClass.hInstance = moduleHandle;
      var classAtom = RegisterClassExW(ref wndClass);
      if (classAtom == IntPtr.Zero) {
        var lastError = Marshal.GetLastWin32Error();
        Log.Error($"Could not register window class (code {lastError})");
        return;
      }
      handle = CreateWindowExW(
        0,
        classAtom,
        String.Empty,
        0,
        0,
        0,
        0,
        0,
        HWND_MESSAGE,
        IntPtr.Zero,
        moduleHandle,
        IntPtr.Zero
      );
      if (handle == IntPtr.Zero) {
        var lastError = Marshal.GetLastWin32Error();
        Log.Error($"Could create window with class {classAtom} (code {lastError})");
        return;
      }
      System.Windows.Forms.Application.Run();
    });
  }

  public static void Stop() {
    DestroyWindow(handle);
    handle = IntPtr.Zero;
    thread.Join();
  }

  public static void Invoke(string name, Action action) {
    invocations.Add(new Invocation {
      name = name,
      action = action,
      threadId = Thread.CurrentThreadId,
    });
    Notify();
  }

  // Internal methods
  ///////////////////////

  static void Notify() {
    PostMessage(handle, WM_USER + 1, IntPtr.Zero, IntPtr.Zero);
  }

  static IntPtr OnWndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam) {
    if (msg >= WM_USER && msg < WM_USER + 0x8000) {
      while (invocations.TryTake(out var invocation)) Run(invocation);
    }
    return DefWindowProcW(hWnd, msg, wParam, lParam);
  }

  static void Run(Invocation invocation) {
    try {
      Log.Trace($"running event loop invocation {invocation.name} [{invocation.threadId}]");
      invocation.action();
    } catch (Exception e) {
      Log.Error($"exception encountered for event loop invocation {invocation.name} [{invocation.threadId}]\n{e.ToString()}");
    }
  }

}

}
