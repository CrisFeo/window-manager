using System;
using System.Collections.Generic;

using WinMutex = System.Threading.Mutex;

namespace WinCtl {

public static class Lock {

  // Classes
  ///////////////////////

  public class Mutex : IDisposable {

    WinMutex mutex;

    public Mutex() {
      mutex = new WinMutex();
    }

    public void Dispose() {
      mutex.Dispose();
    }

    public void Acquire() {
      mutex.WaitOne();
    }

    public void Release() {
      mutex.ReleaseMutex();
    }

  }

  public class HeldMutex : IDisposable {

    Mutex instance;

    public HeldMutex(Mutex instance) {
      this.instance = instance;
      instance.Acquire();
    }

    public void Dispose() {
      instance.Release();
    }

  }

  // Internal vars
  ///////////////////////

  static List<Mutex> instances = new List<Mutex>();

  // Public methods
  ///////////////////////


  public static void DisposeAll() {
    foreach (var i in instances) i.Dispose();
    instances.Clear();
  }

  public static Mutex New() {
    var instance = new Mutex();
    instances.Add(instance);
    return instance;
  }

  public static HeldMutex Acquire(Mutex instance) {
    return new HeldMutex(instance);
  }

}

}
