using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

static class Hotkey {

  // Constants
  ///////////////////////

  static readonly HashSet<Key> ALT_KEYS = new HashSet<Key> {
    Key.LeftMenu,
    Key.RightMenu,
  };

  static readonly HashSet<Key> CTRL_KEYS = new HashSet<Key> {
    Key.LeftControl,
    Key.RightControl,
  };

  static readonly HashSet<Key> SHIFT_KEYS = new HashSet<Key> {
    Key.LeftShift,
    Key.RightShift,
  };

  static readonly HashSet<Key> WIN_KEYS = new HashSet<Key> {
    Key.LeftWindows,
    Key.RightWindows,
  };

  static readonly HashSet<Key> INVALID_KEYS = new HashSet<Key> {
    Key.None,
    Key.LeftMenu,
    Key.RightMenu,
    Key.LeftControl,
    Key.RightControl,
    Key.LeftShift,
    Key.RightShift,
    Key.LeftWindows,
    Key.RightWindows,
  };

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
  static HashSet<Key> heldKeys;
  static Mod heldMods;

  // Public methods
  ///////////////////////

  public static bool Map(Mod mods, Key key, Action callback) {
    Initialize();
    if (mods == Mod.None || INVALID_KEYS.Contains(key)) return false;
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
    heldKeys = new HashSet<Key>();
  }

  static bool OnDown(Key key) {
    if (ALT_KEYS.Contains(key))   { heldMods |= Mod.Alt;   return false; }
    if (CTRL_KEYS.Contains(key))  { heldMods |= Mod.Ctrl;  return false; }
    if (SHIFT_KEYS.Contains(key)) { heldMods |= Mod.Shift; return false; }
    if (WIN_KEYS.Contains(key))   { heldMods |= Mod.Win;   return false; }
    if (heldKeys.Add(key)) {
      var handler = FindHandler(heldMods, key);
      if (handler != null) {
        handler();
        return true;
      }
    }
    return false;
  }

  static bool OnUp(Key key) {
    if (ALT_KEYS.Contains(key))   { heldMods &= ~Mod.Alt;   return false; }
    if (CTRL_KEYS.Contains(key))  { heldMods &= ~Mod.Ctrl;  return false; }
    if (SHIFT_KEYS.Contains(key)) { heldMods &= ~Mod.Shift; return false; }
    if (WIN_KEYS.Contains(key))   { heldMods &= ~Mod.Win;   return false; }
    heldKeys.Remove(key);
    return false;
  }

  static Action FindHandler(Mod mods, Key key) {
    if (!handlers.ContainsKey(mods)) return null;
    if (!handlers[mods].ContainsKey(key)) return null;
    return handlers[mods][key];
  }

}
