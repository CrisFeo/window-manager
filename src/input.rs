use windows_sys::Win32::UI::Input::KeyboardAndMouse::{
  SendInput,
  INPUT,
  INPUT_KEYBOARD,
  INPUT_0,
  KEYBDINPUT,
  KEYEVENTF_SCANCODE,
  KEYEVENTF_KEYUP,
};
use crate::keys::Key;

pub fn send(key: Key, pressed: bool) {
  unsafe {
    let scan_code = key.to_scan_code();
    let mut flags = KEYEVENTF_SCANCODE;
    if !pressed {
      flags |= KEYEVENTF_KEYUP;
    }
    let input = INPUT {
      r#type: INPUT_KEYBOARD,
      Anonymous: INPUT_0 {
        ki: KEYBDINPUT {
          wVk: 0,
          wScan: scan_code as u16,
          dwFlags: flags,
          time: 0,
          dwExtraInfo: 0,
        },
      },
    };
    SendInput(
      1,
      &input,
      size_of::<INPUT>() as i32,
    );
  }
}
