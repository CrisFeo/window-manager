using System;
using System.Threading;
using System.Runtime.InteropServices;

using WinThread = System.Threading.Thread;

namespace WinCtl {

static class Thread {

  // Classes
  ///////////////////////

  public class Basic {

    public WinThread thread;

    public void Join() {
      thread.Join();
    }

  }

  public class EventLoop {

    public WinThread thread;
    public bool isJoining;

    public void Join() {
      isJoining = true;
      thread.Join();
    }

  }

  // DLL imports
  ///////////////////////

  [DllImport("user32.dll")]
  static extern bool GetMessage(IntPtr msg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

  [DllImport("user32.dll")]
  static extern bool TranslateMessage(IntPtr msg);

  [DllImport("user32.dll")]
  static extern long DispatchMessage(IntPtr msg);

  // Public properties
  ///////////////////////

  public static int CurrentThreadId {
    get => WinThread.CurrentThread.ManagedThreadId;
  }

  // Public methods
  ///////////////////////

  public static Basic Run(string name, Action action) {
    var basic = new Basic();
    basic.thread = new WinThread(() => {
      Log.Info($"started basic thread with name {name}");
      action();
    });
    basic.thread.SetApartmentState(ApartmentState.STA);
    basic.thread.Start();
    return basic;
  }

  public static EventLoop RunWithEventLoop(string name, Action action) {
    var eventLoop = new EventLoop();
    eventLoop.thread = new WinThread(() => {
      Log.Info($"started event thread with name {name}");
      action();
      var msg = new IntPtr();
      while (!eventLoop.isJoining && GetMessage(msg, IntPtr.Zero, 0, 0)) {
        TranslateMessage(msg);
        DispatchMessage(msg);
      }
    });
    eventLoop.thread.SetApartmentState(ApartmentState.STA);
    eventLoop.thread.Start();
    return eventLoop;
  }

}

}

