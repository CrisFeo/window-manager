using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VirtualDesktops {

public static class Guids {
  public static readonly Guid CLSID_ImmersiveShell = new Guid("C2F03A33-21F5-47FA-B4BB-156362A2F239");
  public static readonly Guid CLSID_VirtualDesktopManagerInternal = new Guid("C5E0CDCA-7B6E-41B2-9FC4-D93975CC467B");
  public static readonly Guid CLSID_VirtualDesktopManager = new Guid("AA509086-5CA9-4C25-8F95-589D3C07B48A");
  public static readonly Guid CLSID_VirtualDesktopPinnedApps = new Guid("B5A399E7-1C87-46B8-88E9-FC5747B171BD");
}

[StructLayout(LayoutKind.Sequential)]
public struct Size {
  public int X;
  public int Y;
}

[StructLayout(LayoutKind.Sequential)]
public struct Rect {
  public int Left;
  public int Top;
  public int Right;
  public int Bottom;
}

public enum APPLICATION_VIEW_CLOAK_TYPE : int {
  AVCT_NONE = 0,
  AVCT_DEFAULT = 1,
  AVCT_VIRTUAL_DESKTOP = 2
}

public enum APPLICATION_VIEW_COMPATIBILITY_POLICY : int {
  AVCP_NONE = 0,
  AVCP_SMALL_SCREEN = 1,
  AVCP_TABLET_SMALL_SCREEN = 2,
  AVCP_VERY_SMALL_SCREEN = 3,
  AVCP_HIGH_SCALE_FACTOR = 4
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("9AC0B5C8-1484-4C5B-9533-4134A0F97CEA")]
public interface IApplicationView {
  int SetFocus();
  int SwitchTo();
  int TryInvokeBack(IntPtr /* IAsyncCallback* */ callback);
  int GetThumbnailWindow(out IntPtr hwnd);
  int GetMonitor(out IntPtr /* IImmersiveMonitor */ immersiveMonitor);
  int GetVisibility(out int visibility);
  int SetCloak(APPLICATION_VIEW_CLOAK_TYPE cloakType, int unknown);
  int GetPosition(ref Guid guid /* GUID for IApplicationViewPosition */, out IntPtr /* IApplicationViewPosition** */ position);
  int SetPosition(ref IntPtr /* IApplicationViewPosition* */ position);
  int InsertAfterWindow(IntPtr hwnd);
  int GetExtendedFramePosition(out Rect rect);
  int GetAppUserModelId([MarshalAs(UnmanagedType.LPWStr)] out string id);
  int SetAppUserModelId(string id);
  int IsEqualByAppUserModelId(string id, out int result);
  int GetViewState(out uint state);
  int SetViewState(uint state);
  int GetNeediness(out int neediness);
  int GetLastActivationTimestamp(out ulong timestamp);
  int SetLastActivationTimestamp(ulong timestamp);
  int GetVirtualDesktopId(out Guid guid);
  int SetVirtualDesktopId(ref Guid guid);
  int GetShowInSwitchers(out int flag);
  int SetShowInSwitchers(int flag);
  int GetScaleFactor(out int factor);
  int CanReceiveInput(out bool canReceiveInput);
  int GetCompatibilityPolicyType(out APPLICATION_VIEW_COMPATIBILITY_POLICY flags);
  int SetCompatibilityPolicyType(APPLICATION_VIEW_COMPATIBILITY_POLICY flags);
  int GetPositionPriority(out IntPtr /* IShellPositionerPriority** */ priority);
  int SetPositionPriority(IntPtr /* IShellPositionerPriority* */ priority);
  int GetSizeConstraints(IntPtr /* IImmersiveMonitor* */ monitor, out Size size1, out Size size2);
  int GetSizeConstraintsForDpi(uint uint1, out Size size1, out Size size2);
  int SetSizeConstraintsForDpi(ref uint uint1, ref Size size1, ref Size size2);
  int QuerySizeConstraintsFromApp();
  int OnMinSizePreferencesUpdated(IntPtr hwnd);
  int ApplyOperation(IntPtr /* IApplicationViewOperation* */ operation);
  int IsTray(out bool isTray);
  int IsInHighZOrderBand(out bool isInHighZOrderBand);
  int IsSplashScreenPresented(out bool isSplashScreenPresented);
  int Flash();
  int GetRootSwitchableOwner(out IApplicationView rootSwitchableOwner);
  int EnumerateOwnershipTree(out IObjectArray ownershipTree);
  int GetEnterpriseId([MarshalAs(UnmanagedType.LPWStr)] out string enterpriseId);
  int IsMirrored(out bool isMirrored);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("2C08ADF0-A386-4B35-9250-0FE183476FCC")]
public interface IApplicationViewCollection {
  int GetViews(out IObjectArray array);
  int GetViewsByZOrder(out IObjectArray array);
  int GetViewsByAppUserModelId(string id, out IObjectArray array);
  int GetViewForHwnd(IntPtr hwnd, out IApplicationView view);
  int GetViewForApplication(object application, out IApplicationView view);
  int GetViewForAppUserModelId(string id, out IApplicationView view);
  int GetViewInFocus(out IntPtr view);
  void outreshCollection();
  int RegisterForApplicationViewChanges(object listener, out int cookie);
  int RegisterForApplicationViewPositionChanges(object listener, out int cookie);
  int UnregisterForApplicationViewChanges(int cookie);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("FF72FFDD-BE7E-43FC-9C03-AD81681E88E4")]
public interface IVirtualDesktop {
  bool IsViewVisible(IApplicationView view);
  Guid GetId();
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("F31574D6-B682-4CDC-BD56-1827860ABEC6")]
public interface IVirtualDesktopManagerInternal {
  int GetCount();
  void MoveViewToDesktop(IApplicationView view, IVirtualDesktop desktop);
  bool CanViewMoveDesktops(IApplicationView view);
  IVirtualDesktop GetCurrentDesktop();
  void GetDesktops(out IObjectArray desktops);
  [PreserveSig]
  int GetAdjacentDesktop(IVirtualDesktop from, int direction, out IVirtualDesktop desktop);
  void SwitchDesktop(IVirtualDesktop desktop);
  IVirtualDesktop CreateDesktop();
  void RemoveDesktop(IVirtualDesktop desktop, IVirtualDesktop fallback);
  IVirtualDesktop FindDesktop(ref Guid desktopid);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("A5CD92FF-29BE-454C-8D04-D82879FB3F1B")]
public interface IVirtualDesktopManager {
  bool IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow);
  Guid GetWindowDesktopId(IntPtr topLevelWindow);
  void MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("4CE81583-1E4C-4632-A621-07A53543148F")]
public interface IVirtualDesktopPinnedApps {
  bool IsAppIdPinned(string appId);
  void PinAppID(string appId);
  void UnpinAppID(string appId);
  bool IsViewPinned(IApplicationView applicationView);
  void PinView(IApplicationView applicationView);
  void UnpinView(IApplicationView applicationView);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9")]
public interface IObjectArray {
  void GetCount(out int count);
  void GetAt(int index, ref Guid iid, [MarshalAs(UnmanagedType.Interface)]out object obj);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
public interface IServiceProvider10 {
  [return: MarshalAs(UnmanagedType.IUnknown)]
  object QueryService(ref Guid service, ref Guid riid);
}

}