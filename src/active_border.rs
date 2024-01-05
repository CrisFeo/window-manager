use std::mem;
use std::ptr;
use std::sync::OnceLock;
use anyhow::{anyhow, Result};
use windows_sys::*;
use windows_sys::Win32::{
  Foundation::*,
  Graphics::Gdi::*,
  System::LibraryLoader::*,
  UI::HiDpi::*,
  UI::WindowsAndMessaging::*,
  UI::Accessibility::*
};
use crate::*;
use crate::colors::Color;
use crate::window::*;

#[derive(Debug, Copy, Clone)]
pub struct Settings {
  pub border_color: Color,
  pub border_radius: i32,
  pub border_size: i32,
}


struct Context {
  handle: HWND,
  settings: Settings,
}

static CONTEXT: OnceLock<Context> = OnceLock::new();

pub fn setup(settings: Settings) -> Result<()> {
  let handle = create_window_handle()?;
  let context = Context{
    handle,
    settings,
  };
  let _ = CONTEXT.set(context);
  set_event_hook(EVENT_SYSTEM_FOREGROUND, Some(event_hook))?;
  set_event_hook(EVENT_OBJECT_LOCATIONCHANGE, Some(event_hook))?;
  Ok(())
}

pub fn force_redraw() {
  let window = Window::from_handle(get_context().handle);
  if let Ok(window) = window {
    window.redraw();
  }
}

fn get_context() -> &'static Context {
  CONTEXT.get().unwrap()
}

fn create_window_handle() -> Result<HWND> {
  let result = unsafe { SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2) };
  if result == 0 {
    return Err(anyhow!("could not set action thread DPI awareness"));
  }
  let instance = win_err!(GetModuleHandleW(ptr::null()))?;
  let class_name = w!("ACTIVE_WINDOW_BORDER");
  let class = WNDCLASSW {
      style: 0,
      lpfnWndProc: Some(window_proc),
      cbClsExtra: 0,
      cbWndExtra: 0,
      hInstance: instance,
      hIcon: 0,
      hCursor: 0,
      hbrBackground: 0,
      lpszMenuName: ptr::null(),
      lpszClassName: class_name,
  };
  win_err!(RegisterClassW(&class))?;
  let style = WS_EX_TOPMOST
    | WS_EX_COMPOSITED
    | WS_EX_TRANSPARENT
    | WS_EX_TOOLWINDOW
    | WS_EX_LAYERED
    | WS_EX_NOACTIVATE;
  let handle = win_err!(CreateWindowExW(
    style,
    class_name,
    ptr::null(),
    WS_VISIBLE | WS_MAXIMIZE | WS_POPUP,
    CW_USEDEFAULT,
    CW_USEDEFAULT,
    CW_USEDEFAULT,
    CW_USEDEFAULT,
    0,
    0,
    instance,
    ptr::null(),
  ))?;
  win_err!(SetLayeredWindowAttributes(handle, 0x0000FFFF, 0, LWA_COLORKEY))?;
  Ok(handle)
}

fn draw_active_border(hdc: HDC, settings: Settings, handle: HWND) -> Result<()> {
  let active = Window::active()?;
  let window = Window::from_handle(handle)?;
  let (screen_w, screen_h) = window.resolution()?;
  unsafe { Rectangle(hdc, 0, 0, screen_w, screen_h) };
  if let Some(a) = active {
    let x = a.rect.x;
    let y = a.rect.y;
    let w = a.rect.w;
    let h = a.rect.h;
    let b = settings.border_size;
    let r = settings.border_radius;
    let is_fullscreen = x == 0 && w == screen_w && y == 0 && h == screen_h;
    if !is_fullscreen {
      unsafe {
        let border_brush = CreateSolidBrush(settings.border_color.0);
        let old_brush = SelectObject(hdc, border_brush);
        RoundRect(hdc, x - b, y - b, x + w + b, y + h + b, r, r);
        SelectObject(hdc, old_brush);
        DeleteObject(border_brush);
        RoundRect(hdc, x, y, x + w, y + h, r, r);
      }
    }
  }
  Ok(())
}

unsafe extern "system" fn window_proc(hwnd: HWND, u_msg: u32, w_param: WPARAM, l_param: LPARAM) -> LRESULT {
  match u_msg {
    WM_PAINT => {
      let mut client_rect = mem::zeroed();
      GetClientRect(hwnd, &mut client_rect);
      let client_w = client_rect.right - client_rect.left;
      let client_h = client_rect.bottom - client_rect.top;
      let mut paint_struct = mem::zeroed();
      let hdc = BeginPaint(hwnd, &mut paint_struct);
      let hdc_mem = CreateCompatibleDC(hdc);
      let hbm_mem = CreateCompatibleBitmap(hdc, client_w, client_h);
      let hbm_old = SelectObject(hdc_mem, hbm_mem);
      let pen = CreatePen(PS_NULL, 0, 0);
      let old_pen = SelectObject(hdc_mem, pen);
      let brush = CreateSolidBrush(0x0000FFFF);
      let old_brush = SelectObject(hdc_mem, brush);
      let settings = get_context().settings;
      let result = draw_active_border(hdc_mem, settings, hwnd);
      if let Err(error) = result {
        println!("failed to draw active window border {error}");
      }
      BitBlt(hdc, 0, 0, client_w, client_h, hdc_mem, 0, 0, SRCCOPY);
      SelectObject(hdc_mem, old_brush);
      DeleteObject(brush);
      SelectObject(hdc_mem, old_pen);
      DeleteObject(pen);
      SelectObject(hdc_mem, hbm_old);
      DeleteObject(hbm_mem);
      DeleteDC(hdc_mem);
      EndPaint(hwnd, &paint_struct);
      0
    },
    _ => DefWindowProcW(hwnd, u_msg, w_param, l_param),
  }
}

fn set_event_hook(event: u32, proc: WINEVENTPROC) -> Result<()> {
  win_err!(SetWinEventHook(
    event,
    event,
    0,
    proc,
    0,
    0,
    WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS,
  ))?;
  Ok(())
}

unsafe extern "system" fn event_hook(
  _hwineventhook: HWINEVENTHOOK,
  _event: u32,
  hwnd: HWND,
  _idobject: i32,
  _idchild: i32,
  _ideventthread: u32,
  _dwmseventtime: u32,
) {
  let window = Window::from_handle(get_context().handle);
  if let Ok(window) = window {
    if let Ok(Some(active)) = Window::active() {
      if active.handle == hwnd {
        window.redraw();
      }
    } else {
      window.redraw();
    }
  }
}
