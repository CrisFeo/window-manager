use std::fmt;
use std::borrow::Cow;
use std::error::Error;
use windows_sys::Win32::Foundation::*;
use windows_sys::Win32::UI::WindowsAndMessaging::*;

pub mod hotkey;
pub mod input;
pub mod keys;
pub mod window;
pub mod desktop;
pub mod colors;

#[derive(Debug)]
pub struct WindowsError(u32, Cow<'static, str>);

impl fmt::Display for WindowsError {
  fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
    write!(f, "windows error (code {}) encountered {}", self.0, self.1)
  }
}

impl Error for WindowsError { }

#[macro_export]
macro_rules! win_err {
  ($call:expr) => {
    {
      unsafe { SetLastError(0) };
      let result = $call;
      if result == 0 {
        let code = unsafe { GetLastError() };
        if code != 0 {
          Err(WindowsError(code, stringify!($call).into()))
        } else {
          Ok(result)
        }
      } else {
        Ok(result)
      }
    }
  }
}

pub fn run(key_event_handler: hotkey::KeyEventHandler) {
  let hotkey_thread = hotkey::setup(key_event_handler).unwrap();
  unsafe {
    let mut message = std::mem::zeroed();
    let mut result;
    loop {
      result = GetMessageW(&mut message, 0, 0, 0);
      DispatchMessageW(&message);
      if result == 0 {
        break;
      }
    }
    println!("EVENT loop exited: {}", result);
  }
  hotkey_thread.join().unwrap();
}
