using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Color = System.Drawing.Color;
using W = Window;
using H = Hotkey;
using M = Hotkey.Mod;
using K = Key;
using G = Graphics;

static class Program {

  // Constants
  ///////////////////////

  const int GAP_SIZE = 20;

  const int BORDER_SIZE = 8;
  const int BORDER_OFFSET = 0;
  static readonly Color BORDER_COLOR = Color.FromArgb(255, 95, 135, 0);

  const long TAP_DURATION = 50;

  const M MOD_PUSH = M.Win | M.Shift;
  const M MOD_FOCUS = M.Win;
  const M MOD_SWITCH = M.Win;

  // Methods
  ///////////////////////

  [STAThread]
  static void Main(string[] args) {
    // Bind keys to switch between virtual desktops
    {
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
    // Bind keys to "push" windows into screen halves with gaps
    {
      var g = GAP_SIZE;
      var hg = GAP_SIZE / 2;
      var ghg = GAP_SIZE + hg;
      Map(MOD_PUSH, K.I, (a, w, h) => W.Move(a, (w-a.w)/2, (h-a.h)/2, null,    null  ));
      Map(MOD_PUSH, K.O, (a, w, h) => W.Move(a, g,         g,         w-2*g,   h-2*g ));
      Map(MOD_PUSH, K.H, (a, w, h) => W.Move(a, g,         null,      w/2-ghg, null  ));
      Map(MOD_PUSH, K.L, (a, w, h) => W.Move(a, w/2+hg,    null,      w/2-ghg, null  ));
      Map(MOD_PUSH, K.K, (a, w, h) => W.Move(a, null,      g,         null,    h/2-ghg));
      Map(MOD_PUSH, K.J, (a, w, h) => W.Move(a, null,      h/2+hg,    null,    h/2-ghg));
    }
    // Bind keys to focus windows adjacent to the active one
    {
      Map(MOD_FOCUS, K.H, a => W.SetActive(W.All()
        .Where(w => w.isVisible)
        .Where(w => w.x < a.x)
        .OrderBy(w => w.x)
        .ThenBy(w => Math.Abs(a.y - w.y))
        .DefaultIfEmpty(a)
        .First()));
      Map(MOD_FOCUS, K.L, a => W.SetActive(W.All()
        .Where(w => w.isVisible)
        .Where(w => w.x > a.x)
        .OrderByDescending(w => w.x)
        .ThenBy(w => Math.Abs(a.y - w.y))
        .DefaultIfEmpty(a)
        .First()));
      Map(MOD_FOCUS, K.K, a => W.SetActive(W.All()
        .Where(w => w.isVisible)
        .Where(w => w.y < a.y)
        .OrderBy(w => w.y)
        .ThenBy(w => Math.Abs(a.x - w.x))
        .DefaultIfEmpty(a)
        .First()));
      Map(MOD_FOCUS, K.J, a => W.SetActive(W.All()
        .Where(w => w.isVisible)
        .Where(w => w.y > a.y)
        .OrderByDescending(w => w.y)
        .ThenBy(w => Math.Abs(a.x - w.x))
        .DefaultIfEmpty(a)
        .First()));
    }
    // Draw a border around the active window
    {
      var o = BORDER_OFFSET + BORDER_SIZE / 2;
      var activeBorderGraphic = G.New(g => {
        var a = W.Active();
        if (!a.isValid) return;
        G.Rect(g, BORDER_COLOR, BORDER_SIZE, a.x-o, a.y-o, a.w+2*o, a.h+2*o);
      });
      Event.onFocus += w => G.Redraw(activeBorderGraphic);
      Event.onMove += w => G.Redraw(activeBorderGraphic);
    }
    // Bind short taps on Shift keys to Parentheses
    {
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
    // Bind Tab to Alt on hold and Tab on tap
    {
      MapTap(
        TAP_DURATION,
        K.Tab,
        new[] { K.LeftMenu },
        new[] { K.Tab }
      );
    }
    // Bind Caps Lock to Control on hold and Escape on tap
    {
      MapTap(
        TAP_DURATION,
        K.CapsLock,
        new[] { K.LeftControl },
        new[] { K.Escape }
      );
    }
    // Bind keys for some useful debugging functionality
    {
#if DEBUG
      Map(M.Win, K.Q, Loop.Exit);
      Map(M.Win, K.W, a => {
        var all = W.All().Where(w => w.isVisible);
        foreach (var w in all) Console.WriteLine(
          $"{W.Title(w)} {W.Class(w)} {w.x},{w.y} {w.w}x{w.h}"
        );
      });
#endif
    }
    Loop.Run();
  }

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

  static void MapTap(long tapDuration, K from, K[] hold, K[] tap) {
    long? downTime = null;
    H.MapDown(M.Any, from, true, () => {
      if (downTime.HasValue) return;
      var thisDownTime = downTime = Time.Now();
      Time.After(tapDuration, () => {
        if (downTime != thisDownTime) return;
        SendRaw(hold.Select(k => (k, true)));
      });
    });
    H.MapUp(M.Any, from, () => {
      if (!downTime.HasValue) return;
      if (Time.Now() - downTime.Value > tapDuration) {
        SendRaw(hold.Select(k => (k, false)));
      } else {
        SendRaw(Enumerable.Concat(
          tap.Select(k => (k, true)),
          tap.Select(k => (k, false))
        ));
      }
      downTime = null;
    });
  }

  static void SendRaw(IEnumerable<(Key, bool)> keystrokes) {
    Input.SendRaw(new LinkedList<(Key, bool)>(keystrokes));
  }

}
