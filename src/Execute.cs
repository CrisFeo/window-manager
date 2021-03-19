using System;
using System.Diagnostics;

using System.IO;
using System.Runtime.InteropServices;

namespace WinCtl {

public static class Execute {

  // Constants
  ///////////////////////

  const string ROOT_PROCESS_NAME = "explorer";
  const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
  const int PROC_THREAD_ATTRIBUTE_PARENT_PROCESS = 0x00020000;

  // Structs
  ///////////////////////

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  struct StartupInfoEx {
    public StartupInfo StartupInfo;
    public IntPtr lpAttributeList;
  }

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
  struct StartupInfo {
    public Int32 cb;
    public string lpReserved;
    public string lpDesktop;
    public string lpTitle;
    public Int32 dwX;
    public Int32 dwY;
    public Int32 dwXSize;
    public Int32 dwYSize;
    public Int32 dwXCountChars;
    public Int32 dwYCountChars;
    public Int32 dwFillAttribute;
    public Int32 dwFlags;
    public Int16 wShowWindow;
    public Int16 cbReserved2;
    public IntPtr lpReserved2;
    public IntPtr hStdInput;
    public IntPtr hStdOutput;
    public IntPtr hStdError;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct ProcessInfomration {
    public IntPtr hProcess;
    public IntPtr hThread;
    public int dwProcessId;
    public int dwThreadId;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct SecurityAttributes {
    public int nLength;
    public IntPtr lpSecurityDescriptor;
    public int bInheritHandle;
  }

  // DLL imports
  ///////////////////////

  [DllImport("kernel32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  static extern bool CreateProcess(
    string lpApplicationName,
    string lpCommandLine,
    ref SecurityAttributes lpProcessAttributes,
    ref SecurityAttributes lpThreadAttributes,
    bool bInheritHandles,
    uint dwCreationFlags,
    IntPtr lpEnvironment,
    string lpCurrentDirectory,
    [In] ref StartupInfoEx lpStartupInfo,
    out ProcessInfomration lpProcessInformation
  );

  [DllImport("kernel32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  static extern bool UpdateProcThreadAttribute(
    IntPtr lpAttributeList,
    uint dwFlags,
    IntPtr Attribute,
    IntPtr lpValue,
    IntPtr cbSize,
    IntPtr lpPreviousValue,
    IntPtr lpReturnSize
  );

  [DllImport("kernel32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  static extern bool InitializeProcThreadAttributeList(
    IntPtr lpAttributeList,
    int dwAttributeCount,
    int dwFlags,
    ref IntPtr lpSize
  );

  [DllImport("kernel32.dll", SetLastError = true)]
  [return: MarshalAs(UnmanagedType.Bool)]
  static extern bool DeleteProcThreadAttributeList(IntPtr lpAttributeList);

  [DllImport("kernel32.dll", SetLastError = true)]
  static extern bool CloseHandle(IntPtr hObject);

  // Public methods
  ///////////////////////

  public static void RunShell(string executable, string arguments) {
    var p = new Process();
    p.StartInfo.FileName = executable;
    p.StartInfo.Arguments = arguments;
    p.StartInfo.UseShellExecute = false;
    p.Start();
  }

  public static void RunProc(string executable, string arguments) {
    var p = Process.GetProcessesByName(ROOT_PROCESS_NAME)[0];
    var error = RunUnderParent(executable, arguments, p.Id);
    if (error != null) {
      Log.Error(error);
    }
  }

  // Internal methods
  ///////////////////////

  static string RunUnderParent(string executable, string arguments, int parentProcessId) {
    var pInfo = new ProcessInfomration();
    var sInfoEx = new StartupInfoEx();
    sInfoEx.StartupInfo.cb = Marshal.SizeOf(sInfoEx);
    var lpValue = IntPtr.Zero;
    try {
      var lpSize = IntPtr.Zero;
      var success = InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref lpSize);
      if (success || lpSize == IntPtr.Zero) {
        return $"could not get process thread attribute list size";
      }
      sInfoEx.lpAttributeList = Marshal.AllocHGlobal(lpSize);
      success = InitializeProcThreadAttributeList(sInfoEx.lpAttributeList, 1, 0, ref lpSize);
      if (!success) {
        return $"error in InitializeProcThreadAttributeList: {Marshal.GetLastWin32Error()}";
      }
      var parentHandle = Process.GetProcessById(parentProcessId).Handle;
      lpValue = Marshal.AllocHGlobal(IntPtr.Size);
      Marshal.WriteIntPtr(lpValue, parentHandle);
      success = UpdateProcThreadAttribute(
        sInfoEx.lpAttributeList,
        0,
        (IntPtr)PROC_THREAD_ATTRIBUTE_PARENT_PROCESS,
        lpValue,
        (IntPtr)IntPtr.Size,
        IntPtr.Zero,
        IntPtr.Zero);
      if (!success) {
        return $"error in UpdateProcThreadAttribute: {Marshal.GetLastWin32Error()}";
      }
      var pSec = new SecurityAttributes();
      var tSec = new SecurityAttributes();
      pSec.nLength = Marshal.SizeOf(pSec);
      tSec.nLength = Marshal.SizeOf(tSec);
      success = CreateProcess(
        executable,
        " " + arguments,
        ref pSec,
        ref tSec,
        false,
        EXTENDED_STARTUPINFO_PRESENT,
        IntPtr.Zero,
        null,
        ref sInfoEx,
        out pInfo
      );
      if (!success) {
        return $"error in CreateProcess: {Marshal.GetLastWin32Error()}";
      }
      return null;
    } finally {
      if (sInfoEx.lpAttributeList != IntPtr.Zero) {
        DeleteProcThreadAttributeList(sInfoEx.lpAttributeList);
        Marshal.FreeHGlobal(sInfoEx.lpAttributeList);
      }
      Marshal.FreeHGlobal(lpValue);
      if (pInfo.hProcess != IntPtr.Zero) {
        CloseHandle(pInfo.hProcess);
      }
      if (pInfo.hThread != IntPtr.Zero) {
        CloseHandle(pInfo.hThread);
      }
    }
  }

}

}
