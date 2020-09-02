using System;
using System.Collections.Generic;

namespace WinCtl {

public static class Hotkey {

  // Constants
  ///////////////////////

  internal static readonly (Key, bool) DISABLE_KEYSTROKE = (DISABLE_KEY, false);

  const Key DISABLE_KEY = Key.Undefined;

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

  // Enums
  ///////////////////////

  [Flags]
  public enum Mod {
    None = 0,
    Alt = 1,
    Ctrl = 2,
    Shift = 4,
    Win = 8,
    Any = 16,
  }

  // Structs
  ///////////////////////

  struct DownHandler {
    public Action action;
    public bool isRepeating;
  }

  // Internal vars
  ///////////////////////

  static Dictionary<Mod, Dictionary<Key, DownHandler>> downHandlers;
  static Dictionary<Mod, Dictionary<Key, Action>> upHandlers;
  static HashSet<Key> heldKeys;
  static Mod heldMods;
  static bool isDisabled;

  // Static constructor
  ///////////////////////

  static Hotkey() {
    KeyHook.Install(OnDown, OnUp);
    downHandlers = new Dictionary<Mod, Dictionary<Key, DownHandler>>();
    upHandlers = new Dictionary<Mod, Dictionary<Key, Action>>();
    heldKeys = new HashSet<Key>();
  }

  // Public methods
  ///////////////////////

  public static bool MapDown(
    Mod mods,
    Key key,
    bool isRepeating,
    Action action
  ) {
    if (!downHandlers.ContainsKey(mods)) {
      downHandlers[mods] = new Dictionary<Key, DownHandler>();
    }
    if (downHandlers[mods].ContainsKey(key)) return false;
    downHandlers[mods][key] = new DownHandler {
      action = action,
      isRepeating = isRepeating,
    };
    return true;
  }

  public static bool MapUp(Mod mods, Key key, Action action) {
    if (!upHandlers.ContainsKey(mods)) {
      upHandlers[mods] = new Dictionary<Key, Action>();
    }
    if (upHandlers[mods].ContainsKey(key)) return false;
    upHandlers[mods][key] = action;
    return true;
  }

  public static void Clear() {
    downHandlers.Clear();
    upHandlers.Clear();
  }

  // Internal methods
  ///////////////////////

  static bool OnDown(Key key) {
    if (ALT_KEYS.Contains(key))   heldMods |= Mod.Alt;
    if (CTRL_KEYS.Contains(key))  heldMods |= Mod.Ctrl;
    if (SHIFT_KEYS.Contains(key)) heldMods |= Mod.Shift;
    if (WIN_KEYS.Contains(key))   heldMods |= Mod.Win;
    var isRepeat = !heldKeys.Add(key);
    if (isDisabled) return false;
    var (handler, exists) = FindHandler(downHandlers, heldMods, key);
    if (!exists) (handler, exists) = FindHandler(downHandlers, Mod.Any, key);
    if (!exists || handler.action == null) return false;
    if (!handler.isRepeating && isRepeat) return false;
    Loop.Invoke($"hotkey-down {heldMods}-{key}", handler.action);
    return true;
  }

  static bool OnUp(Key key) {
    if (key == DISABLE_KEY) isDisabled = !isDisabled;
    if (ALT_KEYS.Contains(key))   heldMods &= ~Mod.Alt;
    if (CTRL_KEYS.Contains(key))  heldMods &= ~Mod.Ctrl;
    if (SHIFT_KEYS.Contains(key)) heldMods &= ~Mod.Shift;
    if (WIN_KEYS.Contains(key))   heldMods &= ~Mod.Win;
    if (!heldKeys.Remove(key)) return false;
    if (isDisabled) return false;
    var (handler, exists) = FindHandler(upHandlers, heldMods, key);
    if (!exists) (handler, exists) = FindHandler(upHandlers, Mod.Any, key);
    if (!exists || handler == null) return false;
    Loop.Invoke($"hotkey-up {heldMods}-{key}", handler);
    return true;
  }

  static (T, bool) FindHandler<T>(
    Dictionary<Mod, Dictionary<Key, T>> handlers,
    Mod mod,
    Key key
  ) {
    if (!handlers.ContainsKey(mod))      return (default, false);
    if (!handlers[mod].ContainsKey(key)) return (default, false);
    return (handlers[mod][key], true);
  }

}

}
