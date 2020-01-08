using System;
using System.Runtime.InteropServices;
using System.Linq;

using W = Window;
using H = Hotkey;
using K = Key;

static class Program {

  // Constants
  ///////////////////////

  const int BORDER_SIZE = 30;
  const H.Mod MOD_MOVE = H.Mod.Win | H.Mod.Shift;
  const H.Mod MOD_FOCUS = H.Mod.Win;

  // Methods
  ///////////////////////

  static void Main(string[] args) {
    W.Initialize();
    var b = BORDER_SIZE;
    var hb = BORDER_SIZE / 2;
    var bb = BORDER_SIZE + hb;
    Map(MOD_MOVE, K.I, (a, w, h) => W.Move(a, (w-a.w)/2, (h-a.h)/2, null,   null  ));
    Map(MOD_MOVE, K.O, (a, w, h) => W.Move(a, b,         b,         w-2*b,  h-2*b ));
    Map(MOD_MOVE, K.H, (a, w, h) => W.Move(a, b,         null,      w/2-bb, null  ));
    Map(MOD_MOVE, K.L, (a, w, h) => W.Move(a, w/2+hb,    null,      w/2-bb, null  ));
    Map(MOD_MOVE, K.K, (a, w, h) => W.Move(a, null,      b,         null,   h/2-bb));
    Map(MOD_MOVE, K.J, (a, w, h) => W.Move(a, null,      h/2+hb,    null,   h/2-bb));
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
#if DEBUG
    H.Register(H.Mod.Win, K.Q, () => false);
#endif
    H.Listen();
  }

  static void Map(H.Mod mod, K key, Action<W.Info> fn) {
    H.Register(mod, key, () => {
      var active = W.Active(); if (!active.Valid) return;
      fn(active);
    });
  }

  static void Map(H.Mod mod, K key, Action<W.Info, int, int> fn) {
    H.Register(mod, key, () => {
      var active = W.Active(); if (!active.Valid) return;
      var (w, h) = W.Resolution();
      fn(active, w, h);
    });
  }

}
