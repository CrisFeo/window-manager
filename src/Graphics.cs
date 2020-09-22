using System;
using System.Drawing;
using System.Windows.Forms;

using Gfx = System.Drawing.Graphics;
using Clr = System.Drawing.Color;

namespace WinCtl {

public static class Graphics {

  // Constants
  ///////////////////////

  static readonly Clr TRANSPARENCY_COLOR = Clr.Magenta;

  // Structs
  ///////////////////////

  public struct Info {
    public Form form;
  }

  public struct Context {
    internal Gfx graphics;
    public Context(Gfx graphics) {
      this.graphics = graphics;
    }
  }

  public struct Color {
    internal Clr color;
    public Color(int r, int g, int b) {
      color = Clr.FromArgb(255, r, g, b);
    }
  }

  // Classes
  ///////////////////////

  class GraphicsForm : Form {

    const int WS_EX_TOPMOST     = 0x00000008;
    const int WS_EX_TRANSPARENT = 0x00000020;
    const int WS_EX_TOOLWINDOW  = 0x00000080;
    const int WS_EX_LAYERED     = 0x00080000;
    const int WS_EX_NOACTIVATE  = 0x08000000;

    public GraphicsForm() {
      FormBorderStyle = FormBorderStyle.None;
      Bounds = Screen.PrimaryScreen.Bounds;
      ShowInTaskbar = false;
      ControlBox = false;
      DoubleBuffered = true;
      TransparencyKey = TRANSPARENCY_COLOR;
      BackColor = TRANSPARENCY_COLOR;
    }

    protected override bool ShowWithoutActivation {
      get => true;
    }

    protected override CreateParams CreateParams {
      get {
        var p = base.CreateParams;
        p.ExStyle |= WS_EX_TOPMOST;
        p.ExStyle |= WS_EX_TRANSPARENT;
        p.ExStyle |= WS_EX_TOOLWINDOW;
        p.ExStyle |= WS_EX_LAYERED;
        p.ExStyle |= WS_EX_NOACTIVATE;
        return p;
      }
    }

  }

  // Public methods
  ///////////////////////

  public static Info New(Action<Context> onPaint) {
    var form = new GraphicsForm();
    form.Paint += (s, e) => onPaint(new Context(e.Graphics));
    var t = Thread.Run("graphics", () => Application.Run(form));
    return new Info { form = form };
  }

  public static void Close(Info info) {
    info.form.Close();
  }

  public static void Redraw(Info info) {
    info.form.Refresh();
  }

  public static void Rect(
    Context ctx,
    Color c,
    int t,
    int x,
    int y,
    int w,
    int h
  ) {
    ctx.graphics.DrawRectangle(new Pen(c.color, t), x, y, w, h);
  }

}

}
