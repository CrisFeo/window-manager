using System;
using System.Drawing;
using System.Windows.Forms;

using Gfx = System.Drawing.Graphics;

static class Graphics {

  // Constants
  ///////////////////////

  static readonly Color TRANSPARENCY_COLOR = Color.Magenta;

  // Structs
  ///////////////////////

  public struct Info {
    public Form form;
  }

  // Classes
  ///////////////////////

  class GraphicsForm : Form {

    const int WS_EX_NOACTIVATE = 0x08000000;
    const int WS_EX_TOOLWINDOW = 0x00000080;
    const int WS_EX_TOPMOST    = 0x00000008;

    public GraphicsForm() {
      FormBorderStyle = FormBorderStyle.FixedToolWindow;
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
        p.ExStyle |= WS_EX_NOACTIVATE;
        p.ExStyle |= WS_EX_TOOLWINDOW;
        p.ExStyle |= WS_EX_TOPMOST;
        return p;
      }
    }

  }

  // Public methods
  ///////////////////////

  public static Info New(Action<Gfx> onPaint) {
    var form = new GraphicsForm();
    form.Paint += (s, e) => onPaint(e.Graphics);
    form.Visible = true;
    form.Refresh();
    return new Info { form = form };
  }

  public static void Redraw(Info info) {
    info.form.Refresh();
  }

  public static void Rect(
    Gfx g,
    Color c,
    int t,
    int x,
    int y,
    int w,
    int h
  ) {
    g.DrawRectangle(new Pen(c, t), x, y, w, h);
  }

}
