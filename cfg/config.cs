using System;
using System.Collections.Generic;
using System.Linq;
using WinCtl;

using W = WinCtl.Window;
using H = WinCtl.Hotkey;
using M = WinCtl.Hotkey.Mod;
using K = WinCtl.Key;
using G = WinCtl.Graphics;
using Color = WinCtl.Graphics.Color;

static class Script {

  // Constants
  ///////////////////////

  const int GAP_SIZE = 20;

  const int BORDER_SIZE = 8;
  const int BORDER_OFFSET = 0;
  static readonly Color BORDER_COLOR = new Color(95, 135, 0);
  static readonly HashSet<string> BORDER_IGNORE_TITLES = new HashSet<string> {
    "Cortana",
  };

  const int TAP_DURATION = 100;

  const M MOD_PUSH = M.Win | M.Shift;
  const M MOD_FOCUS = M.Win;
  const M MOD_SWITCH = M.Win;
  const M MOD_VI = M.Ctrl | M.Alt;

  // Structs
  ///////////////////////

  enum Mode {
    General,
    Game,
  }

  // Internal vars
  ///////////////////////

  static Mode mode = Mode.General;
  static G.Info? activeBorderGraphic;

  // Methods
  ///////////////////////

  static void Main() {
    SetMode();
  }

  static void SetMode() {
    H.Clear();
    Event.Clear();
    if (activeBorderGraphic.HasValue) {
      G.Close(activeBorderGraphic.Value);
      activeBorderGraphic = null;
    }
    switch (mode) {
      case Mode.General: {
        ModeSwitcher();
        VirtualDesktop();
        WindowArrangement();
        WindowFocus();
        WindowBorder();
        ShiftParentheses();
        TabAlt();
        CapsControl();
        ViKeys();
        break;
      }
      case Mode.Game: {
        ModeSwitcher();
        VirtualDesktop();
        break;
      }
    }
  }

  static void ModeSwitcher() {
    Map(M.Win, K.F12, () => {
      switch (mode) {
        case Mode.General: mode = Mode.Game;    break;
        case Mode.Game:    mode = Mode.General; break;
      }
      SetMode();
    });
  }

  static void VirtualDesktop() {
    Map(MOD_SWITCH, K.N1, () => Desktop.GoTo(0));
    Map(MOD_SWITCH, K.N2, () => Desktop.GoTo(1));
    Map(MOD_SWITCH, K.N3, () => Desktop.GoTo(2));
    Map(MOD_SWITCH, K.N4, () => Desktop.GoTo(3));
    Map(MOD_SWITCH, K.N5, () => Desktop.GoTo(4));
    Map(MOD_SWITCH, K.N6, () => Desktop.GoTo(5));
    Map(MOD_SWITCH, K.N7, () => Desktop.GoTo(6));
    Map(MOD_SWITCH, K.N8, () => Desktop.GoTo(7));
    Map(MOD_SWITCH, K.N9, () => Desktop.GoTo(8));
  }

  static void WindowArrangement() {
    var g = GAP_SIZE;
    var hg = GAP_SIZE / 2;
    var ghg = GAP_SIZE + hg;
    Map(MOD_PUSH, K.Y, (a, w, h) => W.Move(a, 0,         0,         w,       h     ));
    Map(MOD_PUSH, K.U, (a, w, h) => W.Move(a, g,         g,         w-2*g,   h-2*g ));
    Map(MOD_PUSH, K.I, (a, w, h) => W.Move(a, (w-a.w)/2, (h-a.h)/2, null,    null  ));
    Map(MOD_PUSH, K.H, (a, w, h) => W.Move(a, g,         null,      w/2-ghg, null  ));
    Map(MOD_PUSH, K.L, (a, w, h) => W.Move(a, w/2+hg,    null,      w/2-ghg, null  ));
    Map(MOD_PUSH, K.K, (a, w, h) => W.Move(a, null,      g,         null,    h/2-ghg));
    Map(MOD_PUSH, K.J, (a, w, h) => W.Move(a, null,      h/2+hg,    null,    h/2-ghg));
  }

  static void WindowFocus() {
    Map(MOD_FOCUS, K.OEM1, a => W.SetActive(W.All()
      .Where(w => w.isVisible)
      .Where(w => w != a)
      .Where(w => w.x == a.x && w.y == a.y)
      .DefaultIfEmpty(a)
      .Last()));
    Map(MOD_FOCUS, K.H, a => W.SetActive(W.All()
      .Where(w => w.isVisible)
      .Where(w => w.x < a.x)
      .OrderBy(w => Math.Abs(a.y - w.y))
      .ThenBy(w => Math.Abs(a.x - w.x))
      .DefaultIfEmpty(a)
      .First()));
    Map(MOD_FOCUS, K.L, a => W.SetActive(W.All()
      .Where(w => w.isVisible)
      .Where(w => w.x > a.x)
      .OrderBy(w => Math.Abs(a.y - w.y))
      .ThenBy(w => Math.Abs(a.x - w.x))
      .DefaultIfEmpty(a)
      .First()));
    Map(MOD_FOCUS, K.K, a => W.SetActive(W.All()
      .Where(w => w.isVisible)
      .Where(w => w.y < a.y)
      .OrderBy(w => Math.Abs(a.y - w.y))
      .ThenBy(w => Math.Abs(a.x - w.x))
      .DefaultIfEmpty(a)
      .First()));
    Map(MOD_FOCUS, K.J, a => W.SetActive(W.All()
      .Where(w => w.isVisible)
      .Where(w => w.y > a.y)
      .OrderBy(w => Math.Abs(a.y - w.y))
      .ThenBy(w => Math.Abs(a.x - w.x))
      .DefaultIfEmpty(a)
      .First()));
  }

  static void WindowBorder() {
    var o = BORDER_OFFSET + BORDER_SIZE / 2;
    activeBorderGraphic = G.New(c => {
      var a = W.Active();
      if (!a.isValid) return;
      if (BORDER_IGNORE_TITLES.Contains(W.Title(a))) return;
      G.Rect(c, BORDER_COLOR, BORDER_SIZE, a.x-o, a.y-o, a.w+2*o, a.h+2*o);
    });
    Event.onFocus += w => G.Redraw(activeBorderGraphic.Value);
    Event.onMove += w => G.Redraw(activeBorderGraphic.Value);
  }

  static void ShiftParentheses() {
    MapTap(
      TAP_DURATION,
      K.LeftShift,
      new[] { K.LeftShift },
      new[] { K.LeftShift, K.N9 }
    );
    MapTap(
      TAP_DURATION,
      K.RightShift,
      new[] { K.RightShift },
      new[] { K.RightShift, K.N0 }
    );
  }

  static void TabAlt() {
    MapTapDelayHold(
      TAP_DURATION,
      K.Tab,
      new[] { K.RightMenu },
      new[] { K.Tab }
    );
  }

  static void CapsControl() {
    MapTap(
      TAP_DURATION,
      K.CapsLock,
      new[] { K.LeftControl },
      new[] { K.Escape }
    );
  }

  static void ViKeys() {
    Action<K> Arrow = k => Send(new[] { (k, true), (k, false) });
    H.MapDown(MOD_VI, K.H, true, () => Arrow(K.Left));
    H.MapDown(MOD_VI, K.J, true, () => Arrow(K.Down));
    H.MapDown(MOD_VI, K.K, true, () => Arrow(K.Up));
    H.MapDown(MOD_VI, K.L, true, () => Arrow(K.Right));
    H.MapDown(MOD_VI, K.Up,   true, () => Arrow(K.Prior));
    H.MapDown(MOD_VI, K.Down, true, () => Arrow(K.Next));
  }

  // Helper methods
  ///////////////////////

  static void Map(M mod, K key, Action fn) {
    H.MapDown(mod, key, false, fn);
  }

  static void Map(M mod, K key, Action<W.Info> fn) {
    H.MapDown(mod, key, false, () => {
      var active = W.Active();
      if (!active.isValid) return;
      fn(active);
    });
  }

  static void Map(M mod, K key, Action<W.Info, int, int> fn) {
    H.MapDown(mod, key, false, () => {
      var active = W.Active();
      if (!active.isValid) return;
      var (w, h) = W.Resolution();
      fn(active, w, h);
    });
  }

  static void MapTap(int tapMs, K from, K[] hold, K[] tap) {
    var tapDuration = tapMs / 1000f;
    var holdDown = hold.Select(k => (k, true));
    var holdUp = hold.Select(k => (k, false));
    var tapPress = holdUp
      .Concat(tap.Select(k => (k, true)))
      .Concat(tap.Select(k => (k, false)));
    var l = Lock.New();
    var downTime = default(float?);
    H.MapDown(M.Any, from, true, () => {
      using (Lock.Acquire(l)) {
        if (downTime.HasValue) return;
        downTime = Time.Now();
        SendRaw(holdDown);
      }
    });
    H.MapUp(M.Any, from, () => {
      using (Lock.Acquire(l)) {
        if (downTime.HasValue && Time.Now() - downTime.Value <= tapDuration) {
          SendRaw(tapPress);
        } else {
          SendRaw(holdUp);
        }
        downTime = null;
      }
    });
  }

  static void MapTapDelayHold(int tapMs, K from, K[] hold, K[] tap) {
    var tapDuration = tapMs / 1000f;
    var holdDown = hold.Select(k => (k, true));
    var holdUp = hold.Select(k => (k, false));
    var tapPress = Enumerable.Concat(
      tap.Select(k => (k, true)),
      tap.Select(k => (k, false))
    );
    var l = Lock.New();
    var downTime = default(float?);
    H.MapDown(M.Any, from, true, () => {
      var thisDownTime = default(float?);
      using (Lock.Acquire(l)) {
        if (downTime.HasValue) return;
        thisDownTime = downTime = Time.Now();
      }
      Time.After(tapDuration, () => {
        using (Lock.Acquire(l)) {
          if (downTime != thisDownTime) return;
          SendRaw(holdDown);
        }
      });
    });
    H.MapUp(M.Any, from, () => {
      using (Lock.Acquire(l)) {
        if (downTime.HasValue && Time.Now() - downTime.Value <= tapDuration) {
          SendRaw(tapPress);
        } else {
          SendRaw(holdUp);
        }
        downTime = null;
      }
    });
  }

  static void Send(IEnumerable<(K, bool)> keystrokes) {
    Input.Send(new LinkedList<(K, bool)>(keystrokes));
  }

  static void SendRaw(IEnumerable<(K, bool)> keystrokes) {
    Input.SendRaw(new LinkedList<(K, bool)>(keystrokes));
  }

}
