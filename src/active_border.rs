use std::mem;
use std::ptr;
use std::sync::{Mutex, MutexGuard};
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

static CONTEXT: Mutex<Option<Context>> = Mutex::new(None);

pub fn setup(settings: Settings) -> Result<()> {
  let handle = create_window_handle()?;
  let context = Context{
    handle,
    settings,
  };
  *CONTEXT.lock().expect("shouldn't fail to retrieve context during setup") = Some(context);
  set_event_hook(EVENT_SYSTEM_FOREGROUND, Some(event_hook))?;
  set_event_hook(EVENT_OBJECT_LOCATIONCHANGE, Some(event_hook))?;
  Ok(())
}

pub fn force_redraw() {
  let window = get_context().as_ref().map(|c| Window::from_handle(c.handle));
  if let Some(Ok(window)) = window {
    window.redraw();
  }
}

fn get_context() -> MutexGuard<'static, Option<Context>> {
  match CONTEXT.lock() {
    Ok(context) => context,
    Err(_) => panic!("could not obtain context from mutex"),
  }
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

fn draw_active_border(settings: Settings, handle: HWND) -> Result<()> {
  let active = Window::active()?;
  let window = Window::from_handle(handle)?;
  let (screen_w, screen_h) = window.resolution()?;
  unsafe {
    let mut paint_struct = mem::zeroed();
    let hdc = BeginPaint(handle, &mut paint_struct);
    let pen = CreatePen(PS_NULL, 0, 0);
    let transparent_brush = CreateSolidBrush(0x0000FFFF);
    let border_brush = CreateSolidBrush(settings.border_color.0);
    let old_pen = SelectObject(hdc, pen);
    let old_brush = SelectObject(hdc, transparent_brush);
    Rectangle(hdc, 0, 0, screen_w, screen_h);
    if let Some(a) = active {
      let x = a.rect.x;
      let y = a.rect.y;
      let w = {
        let mut w = a.rect.w;
        if x == 0 && w == screen_w {
          w += 1;
        }
        w
      };
      let h = {
        let mut h = a.rect.h;
        if y == 0 && h == screen_h {
          h += 1;
        }
        h
      };
      let b = settings.border_size;
      let r = settings.border_radius;
      SelectObject(hdc, border_brush);
      RoundRect(hdc, x - b, y - b, x + w + b, y + h + b, r, r);
      SelectObject(hdc, transparent_brush);
      RoundRect(hdc, x, y, x + w, y + h, r, r);
    }
    SelectObject(hdc, old_pen);
    SelectObject(hdc, old_brush);
    DeleteObject(pen);
    DeleteObject(transparent_brush);
    DeleteObject(border_brush);
    EndPaint(handle, &paint_struct);
  }
  Ok(())
}

unsafe extern "system" fn window_proc(hwnd: HWND, u_msg: u32, w_param: WPARAM, l_param: LPARAM) -> LRESULT {
  match u_msg {
    WM_PAINT => {
      let settings = get_context().as_ref().map(|c| c.settings);
      if let Some(settings) = settings {
        let result = draw_active_border(settings, hwnd);
        if let Err(error) = result {
          println!("failed to draw active window border {error}");
        }
      }
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
  let window = get_context().as_ref().map(|c| Window::from_handle(c.handle));
  if let Some(Ok(window)) = window {
    if let Ok(Some(active)) = Window::active() {
      if active.handle == hwnd {
        window.redraw();
      }
    } else {
      window.redraw();
    }
  }
}
