use std::collections::HashSet;
use std::sync::{Mutex, MutexGuard, OnceLock};
use std::ptr;
use std::mem;
use std::fmt::Write;
use std::thread::{JoinHandle, spawn};
use std::sync::mpsc::{Sender, channel};
use winapi::ctypes::c_void;
use winapi::shared::windef::{
  HWND,
  POINT,
  RECT,
  DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2,
};
use winapi::shared::minwindef::{
  BOOL,
  TRUE,
  UINT,
  DWORD,
  WPARAM,
  LPARAM,
  LRESULT,
};
use winapi::ctypes::c_int;
use winapi::um::winuser::{
  MSG,
  GetMessageW,
  TranslateMessage,
  DispatchMessageW,
  SetWindowsHookExW,
  CallNextHookEx,
  WH_KEYBOARD_LL,
  KBDLLHOOKSTRUCT,
  WM_KEYUP,
  WM_SYSKEYUP,
  LLKHF_INJECTED,
  EnumWindows,
  GetForegroundWindow,
  GetWindowRect,
  IsWindowVisible,
  GetClassNameW,
  GetWindowLongW,
  GWL_EXSTYLE,
  WS_EX_TOOLWINDOW,
  ShowWindow,
  SW_SHOWNOACTIVATE,
  SetWindowPos,
  SWP_NOZORDER,
  SWP_NOACTIVATE,
  SWP_NOCOPYBITS,
  SWP_NOOWNERZORDER,
  GetWindowThreadProcessId,
  AttachThreadInput,
  BringWindowToTop,
  SW_SHOW,
  GetSystemMetrics,
  SM_CXSCREEN,
  SM_CYSCREEN,
  SetThreadDpiAwarenessContext,
};
use winapi::um::processthreadsapi::GetCurrentThreadId;
use winapi::um::dwmapi::{
  DwmGetWindowAttribute,
  DWMWA_EXTENDED_FRAME_BOUNDS,
  DWMWA_CLOAKED,
};
use winapi::um::errhandlingapi::GetLastError;

#[derive(Debug, Copy, Clone, PartialEq, Eq, Hash)]
enum Key {
  Win,
  Shf,
  Ctl,
  Alt,
  H,
  J,
  K,
  L,
  Y,
  U,
  I,
  Num(u32),
  Code(u32),
}

impl Key {
  fn from_scan_code(scan_code: u32) -> Self {
    use Key::*;
    match scan_code {
      91 => Win,
      42 => Shf,
      29 => Ctl,
      56 => Alt,
      35 => H,
      36 => J,
      37 => K,
      38 => L,
      21 => Y,
      22 => U,
      23 => I,
      11 => Num(0),
      n if (2..=10).contains(&n) => Num(n - 1),
      n => Code(n),
    }
  }
}

#[derive(Debug, Copy, Clone)]
enum Direction {
  Left,
  Down,
  Up,
  Right,
}

#[derive(Debug, Copy, Clone)]
enum Action {
  Focus(Direction),
  Move(Direction),
  MoveFullscreen,
  MoveMaximize,
  MoveCenter,
  Desktop(u32),
}

struct KeyHookContext {
  held_keys: HashSet<Key>,
  action_sender: Option<Sender<Action>>,
}

impl KeyHookContext {
  fn singleton() -> MutexGuard<'static, Self> {
    static CONTEXT: OnceLock<Mutex<KeyHookContext>> = OnceLock::new();
    CONTEXT.get_or_init(|| {
      Mutex::new(Self{
        held_keys: HashSet::new(),
        action_sender: None,
      })
    }).lock().unwrap()
  }

  fn are_held<const N: usize>(&self, keys: [Key; N]) -> bool {
    self.held_keys == HashSet::from(keys)
  }

  fn on_key(&mut self, is_up: bool, key: Key) -> bool {
    let modified = if is_up {
      self.held_keys.remove(&key)
    } else {
      self.held_keys.insert(key)
    };
    if modified {
      println!("LOG: {key:?} {is_up} {:?}", self.held_keys);
      use Key::*;
      let action = match key {
        H if self.are_held([Win]) => Some(Action::Focus(Direction::Left)),
        J if self.are_held([Win]) => Some(Action::Focus(Direction::Down)),
        K if self.are_held([Win]) => Some(Action::Focus(Direction::Up)),
        L if self.are_held([Win]) => Some(Action::Focus(Direction::Right)),
        H if self.are_held([Win, Shf]) => Some(Action::Move(Direction::Left)),
        J if self.are_held([Win, Shf]) => Some(Action::Move(Direction::Down)),
        K if self.are_held([Win, Shf]) => Some(Action::Move(Direction::Up)),
        L if self.are_held([Win, Shf]) => Some(Action::Move(Direction::Right)),
        Y if self.are_held([Win, Shf]) => Some(Action::MoveFullscreen),
        U if self.are_held([Win, Shf]) => Some(Action::MoveMaximize),
        I if self.are_held([Win, Shf]) => Some(Action::MoveCenter),
        Num(0)if self.are_held([Win]) => Some(Action::Desktop(10)),
        Num(n) if self.are_held([Win]) => Some(Action::Desktop(n - 1)),
        _ => None,
      };
      if let Some(action) = action {
        if let Some(action_sender) = &self.action_sender {
          action_sender.send(action).unwrap();
        }
        // TODO
        //return true;
      }
    }
    false
  }

  fn run_action_thread(&mut self) -> JoinHandle<()> {
    let (tx, rx) = channel();
    self.action_sender = Some(tx);
    spawn(move || {
      let result = unsafe { SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2) };
      if result.is_null() {
        panic!("could not set action thread DPI awareness");
      }
      loop {
        let action = rx.recv().unwrap();
        match action {
          Action::Focus(_) => {
            let active = Window::active().unwrap();
            println!("active: actual: {:?} extended: {:?}", active.actual_rect, active.extended_rect);
            let (screen_w, screen_h) = active.resolution().unwrap();
            println!("resolution: {screen_w}x{screen_h}");
            for window in Window::all().unwrap() {
              println!("all: actual: {:?} extended: {:?}", window.actual_rect, window.extended_rect);
            }
          },
          // TODO implement more actions!
          action => println!("ACTION: {action:?}"),
        }
      }
    })
  }
}

type WinResult<T> = Result<T, std::borrow::Cow<'static, str>>;

#[derive(Debug)]
struct Rect {
  x: i32,
  y: i32,
  w: i32,
  h: i32,
}

struct Window {
  handle: HWND,
  actual_rect: Rect,
  extended_rect: Rect,
}

impl Window {
  fn from_handle(handle: HWND) -> WinResult<Option<Self>> {
    let visible = unsafe { IsWindowVisible(handle) == TRUE };
    if !visible {
      return Ok(None);
    }
    let is_cloaked = unsafe {
      let value = Box::new(0);
      let value_ptr = Box::into_raw(value);
      let result = DwmGetWindowAttribute(
        handle,
        DWMWA_CLOAKED,
        value_ptr as *mut c_void,
        mem::size_of::<u32>() as u32,
      );
      if result != 0 {
        return Err(format!("failed to retrieve window cloak status with code: {result}").into());
      }
      *Box::from_raw(value_ptr)
    };
    if is_cloaked != 0 {
      return Ok(None);
    }
    let class_name = unsafe {
      let mut v = Vec::with_capacity(1024);
      let count = GetClassNameW(handle, v.as_mut_ptr(), v.capacity() as i32);
      v.set_len(count as usize);
      String::from_utf16_lossy(&v)
    };
    if class_name == "Progman"
      && class_name == "Shell_TrayWnd"
      && class_name == "Winit Thread Event Target" {
      return Ok(None);
    }
    let extended_styles = unsafe {GetWindowLongW(handle, GWL_EXSTYLE) };
    if extended_styles == 0 {
      let error = unsafe { GetLastError() };
      return Err(format!("failed to retrieve window extended styles with code: {error}").into());
    }
    if extended_styles as u32 & WS_EX_TOOLWINDOW != 0 {
      return Ok(None);
    }
    let mut actual_rect = RECT {
      left: 0,
      top: 0,
      right: 0,
      bottom: 0,
    };
    let result = unsafe { GetWindowRect(handle, &mut actual_rect) };
    if result == 0 {
      let error = unsafe { GetLastError() };
      return Err(format!("failed to retrieve window rect with code: {error}").into());
    }
    let extended_rect = unsafe {
      let value = Box::new(RECT {
        left: 0,
        top: 0,
        right: 0,
        bottom: 0,
      });
      let value_ptr = Box::into_raw(value);
      let result = DwmGetWindowAttribute(
        handle,
        DWMWA_EXTENDED_FRAME_BOUNDS,
        value_ptr as *mut c_void,
        mem::size_of::<RECT>() as u32,
      );
      if result != 0 {
        return Err(format!("failed to retrieve window extended frame bounds with code: {result}").into());
      }
      Box::from_raw(value_ptr)
    };
    let actual_rect = Rect {
      x: actual_rect.left,
      y: actual_rect.top,
      w: actual_rect.right - actual_rect.left,
      h: actual_rect.bottom - actual_rect.top,
    };
    let extended_rect = Rect {
      x: extended_rect.left,
      y: extended_rect.top,
      w: extended_rect.right - extended_rect.left,
      h: extended_rect.bottom - extended_rect.top,
    };
    Ok(Some(Self {
      handle,
      actual_rect,
      extended_rect,
    }))
  }

  fn active() -> WinResult<Self> {
    let handle = unsafe { GetForegroundWindow() };
    if handle.is_null() {
      return Err("active window could not be found".into());
    }
    let window = Self::from_handle(handle)?;
    if let Some(window) = window {
      Ok(window)
    } else {
      Err("active window could not be created from handle".into())
    }
  }

  fn all() -> WinResult<Vec<Self>> {
    let mut results = vec![];
    let mut callback = |handle| {
      results.push(Self::from_handle(handle));
    };
    let mut trait_obj: &mut dyn FnMut(HWND) = &mut callback;
    let closure_pointer_pointer: *mut c_void = unsafe { mem::transmute(&mut trait_obj) };
    let lparam = closure_pointer_pointer as LPARAM;
    unsafe { EnumWindows(Some(enum_windows_callback), lparam) };
    let mut windows = vec![];
    let mut errors = vec![];
    for result in results {
      match result {
        Ok(Some(window)) => windows.push(window),
        Ok(None) => {},
        Err(error) => errors.push(error),
      }
    }
    if !errors.is_empty() {
      let mut composite_error = String::new();
      write!(composite_error, "error creating window(s) from handle").unwrap();
      for error in errors {
        write!(composite_error, "\n  {error}").unwrap();
      }
      Err(composite_error.into())
    } else {
      Ok(windows)
    }
  }

  fn set_rect(&self, new_rect: Rect) -> WinResult<bool> {
    let result = unsafe { ShowWindow(self.handle, SW_SHOWNOACTIVATE) };
    if result == 0 {
      return Ok(false);
    }
    let x = new_rect.x + (self.actual_rect.x - self.extended_rect.x);
    let y = new_rect.y + (self.actual_rect.y - self.extended_rect.y);
    let w = new_rect.w + (self.actual_rect.w - self.extended_rect.w);
    let h = new_rect.h + (self.actual_rect.h - self.extended_rect.h);
    let flags = SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOCOPYBITS | SWP_NOOWNERZORDER;
    let result = unsafe { SetWindowPos(self.handle, ptr::null_mut(), x, y, w, h, flags) };
    if result == 0 {
      let error = unsafe { GetLastError() };
      return Err(format!("failed to set window rect with code: {error}").into());
    }
    Ok(true)
  }

  fn set_active(&self) -> WinResult<bool> {
    let foreground_handle = unsafe { GetForegroundWindow() };
    if foreground_handle.is_null() {
      return Err("foreground window could not be found".into());
    }
    let foreground_thread_id = unsafe { GetWindowThreadProcessId(foreground_handle, ptr::null_mut()) };
    if foreground_thread_id == 0 {
      let error = unsafe { GetLastError() };
      return Err(format!("failed to retrieve foreground window thread id with code: {error}").into());
    }
    let current_thread_id = unsafe { GetCurrentThreadId() };
    if current_thread_id != foreground_thread_id {
      let result = unsafe { AttachThreadInput(current_thread_id, foreground_thread_id, 1) };
      if result == 0 {
        let error = unsafe { GetLastError() };
        return Err(format!("failed to attach thread input with code: {error}").into());
      }
    }
    let result = unsafe { BringWindowToTop(self.handle) };
    if result == 0 {
      let error = unsafe { GetLastError() };
      return Err(format!("failed to bring window to top with code: {error}").into());
    }
    let result = unsafe { ShowWindow(self.handle, SW_SHOW) };
    if result == 0 {
      return Ok(false);
    }
    if current_thread_id != foreground_thread_id {
      let result = unsafe { AttachThreadInput(current_thread_id, foreground_thread_id, 0) };
      if result == 0 {
        let error = unsafe { GetLastError() };
        return Err(format!("failed to detach thread input with code: {error}").into());
      }
    }
    Ok(true)
  }

  fn resolution(&self) -> WinResult<(i32, i32)> {
    let x = unsafe { GetSystemMetrics(SM_CXSCREEN) };
    if x == 0 {
      let error = unsafe { GetLastError() };
      return Err(format!("failed to retrieve x resolution with code: {error}").into());
    }
    let y = unsafe { GetSystemMetrics(SM_CYSCREEN) };
    if y == 0 {
      let error = unsafe { GetLastError() };
      return Err(format!("failed to retrieve y resolution with code: {error}").into());
    }
    Ok((x, y))
  }

}

unsafe extern "system" fn enum_windows_callback(hwnd: HWND, lparam: LPARAM) -> BOOL {
  let closure: &mut &mut dyn FnMut(HWND) = &mut *(lparam as *mut c_void as *mut &mut dyn FnMut(HWND));
  closure(hwnd);
  TRUE
}

unsafe extern "system" fn key_hook(code: c_int, w_param: WPARAM, l_param: LPARAM) -> LRESULT {
  let msg_type = w_param as u32;
  let info = l_param as *mut KBDLLHOOKSTRUCT;
  if code >= 0 && (*info).flags & LLKHF_INJECTED == 0 {
    let mut ctx = KeyHookContext::singleton();
    let handled = ctx.on_key(
      msg_type == WM_KEYUP || msg_type == WM_SYSKEYUP,
      Key::from_scan_code((*info).scanCode),
    );
    if handled {
      return 0;
    }
  }
  CallNextHookEx(ptr::null_mut(), code, w_param, l_param)
}

fn main() {
  let action_thread = KeyHookContext::singleton().run_action_thread();
  unsafe {
    let handle = SetWindowsHookExW(
      WH_KEYBOARD_LL,
      Some(key_hook),
      ptr::null_mut(),
      0,
    );
    if handle.is_null() {
      let code = GetLastError();
      panic!("failed to set key hook with code: {code}");
    }
    let mut msg: MSG = MSG {
      hwnd : 0 as HWND,
      message : 0 as UINT,
      wParam : 0 as WPARAM,
      lParam : 0 as LPARAM,
      time : 0 as DWORD,
      pt : POINT { x: 0, y: 0, },
    };
    loop {
      let result = GetMessageW(&mut msg, 0 as HWND, 0, 0);
      if result <= 0 {
        break;
      }
      TranslateMessage(&msg);
      DispatchMessageW(&msg);
    }
  }
  action_thread.join().unwrap();
}
