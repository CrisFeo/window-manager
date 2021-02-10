using System;
using System.Diagnostics;

namespace WinCtl {

public static class Execute {

  // Public methods
  ///////////////////////

  public static void Run(string path) {
    var process = new Process();
    process.StartInfo.FileName = path;
    process.StartInfo.UseShellExecute = false;
    process.Start();
  }

  public static void ShellRun(string path) {
    var process = new Process();
    process.StartInfo.FileName = path;
    process.StartInfo.UseShellExecute = true;
    process.Start();
  }

}

}
