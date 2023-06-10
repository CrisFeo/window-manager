using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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

  const int TAP_DURATION = 100;

  const int GAP_SIZE = 20;

  const int BORDER_SIZE = 8;
  const int BORDER_OFFSET = 0;
  static readonly Color BORDER_COLOR = new Color(95, 135, 0);
  static readonly HashSet<string> BORDER_IGNORE_TITLES = new HashSet<string> {
    "Cortana",
    "Search",
  };

  static readonly HttpClient HTTP_CLIENT = new HttpClient();

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
        //ShiftParentheses();
        //TabAlt();
        //CapsControl();
        //viKeys();
        Terminal();
        //Clipboard();
        //Launcher();
        break;
      }
      case Mode.Game: {
        ModeSwitcher();
        VirtualDesktop();
        //Launcher();
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
    var mod = M.Win;
    Map(mod, K.N1, () => Desktop.GoTo(0));
    Map(mod, K.N2, () => Desktop.GoTo(1));
    Map(mod, K.N3, () => Desktop.GoTo(2));
    Map(mod, K.N4, () => Desktop.GoTo(3));
    Map(mod, K.N5, () => Desktop.GoTo(4));
    Map(mod, K.N6, () => Desktop.GoTo(5));
    Map(mod, K.N7, () => Desktop.GoTo(6));
    Map(mod, K.N8, () => Desktop.GoTo(7));
    Map(mod, K.N9, () => Desktop.GoTo(8));
  }

  static void WindowArrangement() {
    var mod = M.Win | M.Shift;
    var g = GAP_SIZE;
    var hg = GAP_SIZE / 2;
    var ghg = GAP_SIZE + hg;
    Map(mod, K.Y, (a, w, h) => W.Move(a, 0,         0,         w,       h     ));
    Map(mod, K.U, (a, w, h) => W.Move(a, g,         g,         w-2*g,   h-2*g ));
    Map(mod, K.I, (a, w, h) => W.Move(a, (w-a.w)/2, (h-a.h)/2, null,    null  ));
    Map(mod, K.H, (a, w, h) => W.Move(a, g,         null,      w/2-ghg, null  ));
    Map(mod, K.L, (a, w, h) => W.Move(a, w/2+hg,    null,      w/2-ghg, null  ));
    Map(mod, K.K, (a, w, h) => W.Move(a, null,      g,         null,    h/2-ghg));
    Map(mod, K.J, (a, w, h) => W.Move(a, null,      h/2+hg,    null,    h/2-ghg));
  }

  static void WindowFocus() {
    var mod = M.Win;
    Map(mod, K.OEM1, a => W.SetActive(W.All()
      .Where(w => w.isVisible && w != a)
      .Where(w => w != a)
      .Where(w => w.x == a.x && w.y == a.y)
      .DefaultIfEmpty(a)
      .Last()));
    Map(mod, K.H, a => W.SetActive(W.All()
      .Where(w => w.isVisible && w != a)
      .Where(w => w.x < a.x)
      .OrderBy(w => Math.Abs(a.y - w.y))
      .ThenBy(w => Math.Abs(a.x - w.x))
      .DefaultIfEmpty(a)
      .First()));
    Map(mod, K.L, a => W.SetActive(W.All()
      .Where(w => w.isVisible && w != a)
      .Where(w => w.x > a.x)
      .OrderBy(w => Math.Abs(a.y - w.y))
      .ThenBy(w => Math.Abs(a.x - w.x))
      .DefaultIfEmpty(a)
      .First()));
    Map(mod, K.K, a => W.SetActive(W.All()
      .Where(w => w.isVisible && w != a)
      .Where(w => w.y < a.y)
      .OrderBy(w => Math.Abs(a.y - w.y))
      .ThenBy(w => Math.Abs(a.x - w.x))
      .DefaultIfEmpty(a)
      .First()));
    Map(mod, K.J, a => W.SetActive(W.All()
      .Where(w => w.isVisible && w != a)
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
      new[] { K.Shift },
      new[] { K.Shift, K.N9 }
    );
    MapTap(
      TAP_DURATION,
      K.RightShift,
      new[] { K.Shift },
      new[] { K.Shift, K.N0 }
    );
  }

  static void TabAlt() {
    MapTapDelayHold(
      TAP_DURATION,
      K.Tab,
      new[] { K.Menu },
      new[] { K.Tab }
    );
  }

  static void CapsControl() {
    MapTap(
      TAP_DURATION,
      K.CapsLock,
      new[] { K.Control },
      new[] { K.Escape }
    );
  }

  static void ViKeys() {
    var mod = M.Ctrl | M.Alt;
    Action<K> Arrow = k => Send(new[] { (k, true), (k, false) });
    H.MapDown(mod, K.H,    true, () => Arrow(K.Left));
    H.MapDown(mod, K.J,    true, () => Arrow(K.Down));
    H.MapDown(mod, K.K,    true, () => Arrow(K.Up));
    H.MapDown(mod, K.L,    true, () => Arrow(K.Right));
    H.MapDown(mod, K.Up,   true, () => Arrow(K.Prior));
    H.MapDown(mod, K.Down, true, () => Arrow(K.Next));
  }

  static void Terminal() {
    Map(M.Win, K.T, async () => {
      var body = new Dictionary<string, object>() {
        ["name"] =  "devenv",
        ["directory"] =  "/root",
      };
      var content = new StringContent(
        JsonSerializer.Serialize(body),
        Encoding.UTF8,
        "application/json"
      );
      await HTTP_CLIENT.PostAsync(
        "http://localhost:57689/new-window",
        content
      );
    });
  }

  static void Clipboard() {
    Map(M.Win, K.C, () => ShRun(
      " set -Eeu               ",
      " tmp=\"$(mktemp)\"      ",
      " ~/.bin/clip > \"$tmp\" ",
      " ~/.bin/ks \"$tmp\"     ",
      " ~/.bin/clip < \"$tmp\" "
    ));
  }

  static void Launcher() {
    Map(M.Win, K.Space, () => Execute.RunShell(@"C:\tools\Keypirinha\keypirinha.exe", "--show"));
  }

  // Helper methods
  ///////////////////////

  static void Map(M mod, K key, Action fn) {
    H.MapDown(mod, key, false, fn);
  }

  static void Map(M mod, K key, Func<Task> fn) {
    H.MapDown(mod, key, false, () => Task.Run(fn));
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

  static void ShRun(params string[] lines) {
    var cmd = string.Join("\n", lines).Replace("'", @"\'");
    Execute.RunProc(@"C:\tools\Alacritty\alacritty.exe", $"-e wsl sh -i -c '{cmd}'");
  }

  static void ShDebug(params string[] lines) {
    var cmd = string.Join("\n", lines).Replace("'", @"\'");
    Execute.RunProc(@"C:\tools\Alacritty\alacritty.exe", $"--hold -e wsl sh -i -c '{cmd}'");
  }

}
