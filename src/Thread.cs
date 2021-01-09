using System;
using System.Threading;
using System.Runtime.InteropServices;

using WinThread = System.Threading.Thread;

namespace WinCtl {

static class Thread {

  // Classes
  ///////////////////////

  public class Instance {

    public WinThread thread;

    public void Join() {
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

  public static Instance Run(string name, Action action) {
    var instance = new Instance();
    instance.thread = new WinThread(() => {
      Log.Info($"started thread with name {name}");
      action();
    });
    instance.thread.SetApartmentState(ApartmentState.STA);
    instance.thread.Start();
    return instance;
  }

}

}

