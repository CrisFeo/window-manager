using System;
using System.Runtime.InteropServices;

using W = Window;
using H = Hotkey;

static class Program {

  // Constants
  ///////////////////////

  const int BORDER_SIZE = 20;

  // Methods
  ///////////////////////

  static void Main(string[] args) {
    W.Initialize();
    var mod = H.Mod.Win | H.Mod.Shift;
    var b = BORDER_SIZE;
    var hb = BORDER_SIZE / 2;
    var bb = BORDER_SIZE + hb;
    Map(mod, Key.O, (a, w, h) => W.Move(a, 0,      0,      w,      h     ));
    Map(mod, Key.H, (a, w, h) => W.Move(a, b,      null,   w/2-bb, null  ));
    Map(mod, Key.L, (a, w, h) => W.Move(a, w/2+hb, null,   w/2-bb, null  ));
    Map(mod, Key.K, (a, w, h) => W.Move(a, null,   b,      null,   h/2-bb));
    Map(mod, Key.J, (a, w, h) => W.Move(a, null,   h/2+hb, null,   h/2-bb));
#if DEBUG
    H.Register(mod, Key.Q, () => false);
#endif
    H.Listen();
  }

  static void Map(H.Mod mod, Key key, Action<W.Info, int, int> fn) {
    H.Register(mod, key, () => {
      var active = W.Active(); if (!active.Valid) return;
      var (w, h) = W.Resolution();
      fn(active, w, h);
    });
  }

}
