use std::mem;
use std::ptr;
use std::fmt::Write;
use std::os::raw::c_void;
use anyhow::{anyhow, Result};
use windows_sys::Win32::UI::WindowsAndMessaging::*;
use windows_sys::Win32::Graphics::Dwm::*;
use windows_sys::Win32::Graphics::Gdi::*;
use windows_sys::Win32::System::Threading::*;
use crate::*;

#[derive(Debug, Clone)]
pub struct Rect {
  pub x: i32,
  pub y: i32,
  pub w: i32,
  pub h: i32,
}

pub struct Window {
  pub handle: HWND,
  pub class_name: String,
  pub title: String,
  pub rect: Rect,
  pub offset: Rect,
}

impl Window {
  pub fn from_handle(handle: HWND) -> Result<Self> {
    let class_name = {
      let mut v = Vec::with_capacity(1024);
      let count = win_err!(GetClassNameW(handle, v.as_mut_ptr(), v.capacity() as i32))?;
      unsafe { v.set_len(count as usize) };
      String::from_utf16_lossy(&v)
    };
    let title = {
      let count = win_err!(GetWindowTextLengthW(handle))?;
      let mut v = Vec::with_capacity(count as usize + 1);
      let count = win_err!(GetWindowTextW(handle, v.as_mut_ptr(), v.capacity() as i32))?;
      unsafe { v.set_len(count as usize) };
      String::from_utf16_lossy(&v)
    };
    // Grab the actual window rectangle as reported by windows
    let mut actual_rect = RECT {
      left: 0,
      top: 0,
      right: 0,
      bottom: 0,
    };
    win_err!(GetWindowRect(handle, &mut actual_rect))?;
    let actual_rect = Rect {
      x: actual_rect.left,
      y: actual_rect.top,
      w: actual_rect.right - actual_rect.left,
      h: actual_rect.bottom - actual_rect.top,
    };
    // Grab the "visual" window rectangle used by some modern apps that do non-traditonal
    // things with borders. Notable this included most Windows 11 app (Explorer, Settings, etc)
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
        DWMWA_EXTENDED_FRAME_BOUNDS as u32,
        value_ptr as *mut c_void,
        mem::size_of::<RECT>() as u32,
      );
      if result != 0 {
        return Err(anyhow!("failed to retrieve extended frame bounds with result: {result}"));
      }
      Box::from_raw(value_ptr)
    };
    let extended_rect = Rect {
      x: extended_rect.left,
      y: extended_rect.top,
      w: extended_rect.right - extended_rect.left,
      h: extended_rect.bottom - extended_rect.top,
    };
    // Calculate how far offset the rendered rectangle is from the actual one. We need this
    // later when setting window pos/size as that method operates on the "actual" rectangle.
    let offset_rect = Rect {
      x: actual_rect.x - extended_rect.x,
      y: actual_rect.y - extended_rect.y,
      w: actual_rect.w - extended_rect.w,
      h: actual_rect.h - extended_rect.h,
    };
    Ok(Self {
      handle,
      class_name,
      title,
      rect: extended_rect,
      offset: offset_rect,
    })
  }

  pub fn active() -> Result<Option<Self>> {
    let handle = unsafe { GetForegroundWindow() };
    if handle == 0 {
      return Ok(None);
    }
    let window = Self::from_handle(handle)?;
    if window.is_manageable()? {
      Ok(Some(window))
    } else {
      Ok(None)
    }
  }

  pub fn all() -> Result<Vec<Self>> {
    let mut windows = vec![];
    let mut errors = vec![];
    let mut callback = |handle| {
      let window = Self::from_handle(handle);
      match window {
        Ok(window) => match window.is_manageable() {
          Ok(true) => windows.push(window),
          Ok(false) => {},
          Err(error) => errors.push(error),
        },
        Err(error) => errors.push(error),
      }
    };
    let mut trait_obj: &mut dyn FnMut(HWND) = &mut callback;
    let closure_pointer_pointer: *mut c_void = unsafe { mem::transmute(&mut trait_obj) };
    let lparam = closure_pointer_pointer as LPARAM;
    unsafe { EnumWindows(Some(enum_windows_callback), lparam) };
    if !errors.is_empty() {
      let mut all_errors = String::new();
      for error in errors {
        _ = write!(all_errors, "\n  {error}");
      }
      Err(anyhow!("error creating window(s) from handle: {all_errors}"))
    } else {
      Ok(windows)
    }
  }

  pub fn is_manageable(&self) -> Result<bool> {
    // Non-visible windows should be skipped (they may be on other desktops)
    let visible = unsafe { IsWindowVisible(self.handle) == TRUE };
    if !visible {
      return Ok(false);
    }
    // Newer windows applications could be "cloaked" which prevents them from being visible
    let is_cloaked = unsafe {
      let value = Box::new(0);
      let value_ptr = Box::into_raw(value);
      let result = DwmGetWindowAttribute(
        self.handle,
        DWMWA_CLOAKED as u32,
        value_ptr as *mut c_void,
        mem::size_of::<u32>() as u32,
      );
      if result != 0 {
        return Err(anyhow!("failed to retrieve window cloak status with result: {result}"));
      }
      *Box::from_raw(value_ptr)
    };
    if is_cloaked != 0 {
      return Ok(false);
    }
    // There are some persistent system windows that we should always ignore by class name
    if self.class_name == "Progman"
      || self.class_name == "Shell_TrayWnd"
      || self.class_name == "Winit Thread Event Target" {
      return Ok(false);
    }
    // Ditto with titles
    if self.title == "Cortana"
      || self.title == "Search" {
      return Ok(false);
    }
    // Tool windows show up at the top level but should be ignored - they're not good
    // candidates for window management
    let extended_styles = win_err!(GetWindowLongW(self.handle, GWL_EXSTYLE))?;
    if extended_styles as u32 & WS_EX_TOOLWINDOW != 0 {
      return Ok(false);
    }
    Ok(true)
  }

  pub fn set_rect(&self, rect: Rect) -> Result<bool> {
    let result = unsafe { ShowWindow(self.handle, SW_SHOWNOACTIVATE) };
    if result == 0 {
      return Ok(false);
    }
    let flags = SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOCOPYBITS | SWP_NOOWNERZORDER;
    win_err!(
      SetWindowPos(
        self.handle,
        0,
        rect.x + self.offset.x,
        rect.y + self.offset.y,
        rect.w + self.offset.w,
        rect.h + self.offset.h,
        flags
      )
   )?;
    Ok(true)
  }

  pub fn set_active(&self) -> Result<bool> {
    let foreground_handle = unsafe { GetForegroundWindow() };
    if foreground_handle == 0 {
      return Err(anyhow!("foreground window could not be found"));
    }
    let foreground_thread_id = win_err!(GetWindowThreadProcessId(foreground_handle, ptr::null_mut()))?;
    let current_thread_id = unsafe { GetCurrentThreadId() };
    // If the currently active window isn't owned by the current thread we need to attach
    // to its event queue otherwise we won't have permission to activate it.
    // This is some sort of Microsoft mitigation for applications accidentally stealing
    // focus while they are in the background
    if current_thread_id != foreground_thread_id {
      win_err!(AttachThreadInput(current_thread_id, foreground_thread_id, 1))?;
    }
    win_err!(BringWindowToTop(self.handle))?;
    let result = unsafe { ShowWindow(self.handle, SW_SHOW) };
    if result == 0 {
      return Ok(false);
    }
    if current_thread_id != foreground_thread_id {
      win_err!(AttachThreadInput(current_thread_id, foreground_thread_id, 0))?;
    }
    Ok(true)
  }

  pub fn set_maximized(&self) {
    unsafe { ShowWindow(self.handle, SW_MAXIMIZE) };
  }

  pub fn redraw(&self) {
    unsafe { RedrawWindow(self.handle, ptr::null(), 0, RDW_INVALIDATE) };
  }

  pub fn resolution(&self) -> Result<(i32, i32)> {
    let x = win_err!(GetSystemMetrics(SM_CXSCREEN))?;
    let y = win_err!(GetSystemMetrics(SM_CYSCREEN))?;
    Ok((x, y))
  }

}

unsafe extern "system" fn enum_windows_callback(hwnd: HWND, lparam: LPARAM) -> BOOL {
  let closure: &mut &mut dyn FnMut(HWND) = &mut *(lparam as *mut c_void as *mut &mut dyn FnMut(HWND));
  closure(hwnd);
  TRUE
}
