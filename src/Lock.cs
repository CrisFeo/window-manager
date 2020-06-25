using System;
using System.Collections.Generic;
using System.Threading;

namespace WinCtl {

public static class Lock {

  // Structs
  ///////////////////////

  public class Instance : IDisposable {

    Mutex mutex;

    public Instance() {
      mutex = new Mutex();
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

  public class HeldInstance : IDisposable {

    Instance instance;

    public HeldInstance(Instance instance) {
      this.instance = instance;
      instance.Acquire();
    }

    public void Dispose() {
      instance.Release();
    }

  }

  // Internal vars
  ///////////////////////

  static List<Instance> instances = new List<Instance>();

  // Public methods
  ///////////////////////


  public static void Dispose() {
    foreach (var i in instances) i.Dispose();
    instances.Clear();
  }

  public static Instance New() {
    var instance = new Instance();
    instances.Add(instance);
    return instance;
  }

  public static HeldInstance Acquire(Instance instance) {
    return new HeldInstance(instance);
  }

}

}
