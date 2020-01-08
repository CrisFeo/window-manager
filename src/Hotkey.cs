using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

static class Hotkey {

  // Enums
  ///////////////////////

  [Flags]
  public enum Mod {
    None = 0,
    Alt = 1,
    Ctrl = 2,
    Shift = 4,
    Win = 8
  }

  // Internal vars
  ///////////////////////

  static Dictionary<Mod, Dictionary<Key, Action>> handlers;

  // Public methods
  ///////////////////////

  public static bool Map(Mod mods, Key key, Action callback) {
    Initialize();
    if (mods == Mod.None || key == Key.None) return false;
    if (!handlers.ContainsKey(mods)) {
      handlers[mods] = new Dictionary<Key, Action>();
    }
    if (handlers[mods].ContainsKey(key)) return false;
    handlers[mods][key] = callback;
    return true;
  }

  public static bool Unmap(Mod mods, Key key) {
    Initialize();
    if (!handlers.ContainsKey(mods)) return false;
    var didRemove = handlers[mods].Remove(key);
    if (handlers[mods].Count == 0) handlers.Remove(mods);
    return didRemove;
  }

  // Internal methods
  ///////////////////////

  static void Initialize() {
    if (Hook.IsInstalled) return;
    Hook.Install(OnDown, OnUp);
    handlers = new Dictionary<Mod, Dictionary<Key, Action>>();
  }

  static bool OnDown(Key key) {
    Console.WriteLine($"down: {key}");
    return false;
  }

  static bool OnUp(Key key) {
    Console.WriteLine($"up: {key}");
    return false;
  }

}
