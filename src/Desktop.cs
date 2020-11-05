using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VirtualDesktops;

namespace WinCtl {

public static class Desktop {

  // DLL imports
  ///////////////////////

  [DllImport("user32.dll")]
  static extern bool AllowSetForegroundWindow(int dwProcessId);

  // Internal vars
  ///////////////////////

  static IVirtualDesktopManagerInternal Instance;

  // Static constructor
  ///////////////////////

  static Desktop() {
    var shell = (IServiceProvider10)Activator.CreateInstance(Type.GetTypeFromCLSID(Guids.CLSID_ImmersiveShell));
    Instance = (IVirtualDesktopManagerInternal)shell.QueryService(Guids.CLSID_VirtualDesktopManagerInternal, typeof(IVirtualDesktopManagerInternal).GUID);
  }

  // Public methods
  ///////////////////////

  public static void GoTo(int index) {
    if (index < 0 || index >= Count()) return;
    var presses = new LinkedList<(Key, bool)>();
    presses.AddLast((Key.RightMenu, true));
    presses.AddLast((Key.RightShift, true));
    presses.AddLast((Key.Escape, true));
    presses.AddLast((Key.Escape, false));
    presses.AddLast((Key.RightShift, false));
    presses.AddLast((Key.Escape, true));
    presses.AddLast((Key.Escape, false));
    presses.AddLast((Key.RightMenu, false));
    AllowSetForegroundWindow(-1);
    Instance.SwitchDesktop(GetDesktop(index));
    Input.Send(presses);
  }

  public static int Count() {
    return Instance.GetCount();
  }

  public static int Current() {
    return GetDesktopIndex(Instance.GetCurrentDesktop());
  }

  // Internal methods
  ///////////////////////

  static IVirtualDesktop GetDesktop(int index) {
    var count = Instance.GetCount();
    if (index < 0 || index >= count)
      throw new ArgumentOutOfRangeException("index");
    var desktops = default(IObjectArray);
    Instance.GetDesktops(out desktops);
    var obj = default(object);
    desktops.GetAt(index, typeof(IVirtualDesktop).GUID, out obj);
    Marshal.ReleaseComObject(desktops);
    return (IVirtualDesktop)obj;
  }

  static int GetDesktopIndex(IVirtualDesktop desktop) {
    var index = -1;
    var IdSearch = desktop.GetId();
    var desktops = default(IObjectArray);
    Instance.GetDesktops(out desktops);
    var obj = default(object);
    for (int i = 0; i < Instance.GetCount(); i++) {
      desktops.GetAt(i, typeof(IVirtualDesktop).GUID, out obj);
      if (IdSearch.CompareTo(((IVirtualDesktop)obj).GetId()) == 0) {
        index = i;
        break;
      }
    }
    Marshal.ReleaseComObject(desktops);
    return index;
  }

}

}
