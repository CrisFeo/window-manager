use windows_sys::Win32::UI::Input::KeyboardAndMouse::{
  SendInput,
  GetAsyncKeyState,
  MapVirtualKeyW,
  INPUT,
  INPUT_KEYBOARD,
  INPUT_0,
  KEYBDINPUT,
  KEYEVENTF_SCANCODE,
  KEYEVENTF_KEYUP,
  MAPVK_VSC_TO_VK,
};
use crate::keys::Key;

pub fn send(keys: &[(Key, bool)]) {
  let count = keys
    .len()
    .try_into()
    .expect("could not cast key len into u32");
  let inputs = keys
    .iter()
    .map(|(key, pressed)| {
      let scan_code = key.to_scan_code();
      let mut flags = KEYEVENTF_SCANCODE;
      if !pressed {
        flags |= KEYEVENTF_KEYUP;
      }
      INPUT {
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
      }
    })
    .collect::<Vec<_>>();
  unsafe {
    SendInput(
      count,
      inputs.as_ptr(),
      size_of::<INPUT>() as i32,
    );
  }
}

pub fn is_down(key: Key) -> bool {
  let scan_code = key.to_scan_code();
  let result = unsafe {
    let virtual_key = MapVirtualKeyW(scan_code, MAPVK_VSC_TO_VK);
    let virtual_key = virtual_key
      .try_into()
      .expect("could not cast vk into i32");
    GetAsyncKeyState(virtual_key)
  };
  result < 0
}
