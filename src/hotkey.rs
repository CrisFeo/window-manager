use std::borrow::Cow;
use std::collections::HashSet;
use std::thread::{JoinHandle, spawn};
use std::sync::{Mutex, OnceLock};
use std::sync::mpsc::{Sender, channel};
use anyhow::Result;
use windows_sys::Win32::UI::WindowsAndMessaging::*;
use windows_sys::Win32::UI::HiDpi::*;
use crate::*;
use crate::keys::Key;

pub type KeyEventHandler = Box<dyn Send + Fn(Key, &HashSet<Key>) -> Option<HotkeyAction>>;

pub type HotkeyAction = (Cow<'static, str>, Box<dyn Send + FnOnce() -> Result<()>>);

struct Context {
  key_event_handler: KeyEventHandler,
  held_keys: HashSet<Key>,
  action_sender: Sender<HotkeyAction>,
}

static CONTEXT: OnceLock<Mutex<Context>> = OnceLock::new();

pub fn setup(key_event_handler: KeyEventHandler) -> Result<JoinHandle<()>> {
  let (tx, rx) = channel();
  let context = Context{
    key_event_handler,
    held_keys: HashSet::new(),
    action_sender: tx,
  };
  let _ = CONTEXT.set(Mutex::new(context));
  win_err!(unsafe { SetWindowsHookExW(WH_KEYBOARD_LL, Some(key_hook), 0, 0) })?;
  Ok(spawn(move || {
    let result = unsafe { SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2) };
    if result == 0 {
      panic!("could not set action thread DPI awareness");
    }
    loop {
      match rx.recv() {
        Ok((name, handler)) => {
          println!("ACTION: {name:?}");
          if let Err(error) =  handler() {
            println!("  {error}");
          }
        },
        Err(error) => println!("  {error}"),
      }
    }
  }))
}

unsafe extern "system" fn key_hook(code: i32, w_param: WPARAM, l_param: LPARAM) -> LRESULT {
  let start = std::time::Instant::now();
  let mut handled = false;
  let msg_type = w_param as u32;
  let info = l_param as *mut KBDLLHOOKSTRUCT;
  if code >= 0 && (*info).flags & LLKHF_INJECTED == 0 {
    let mut context = CONTEXT.get().unwrap().try_lock();
    if let Ok(ref mut context) = context {
      let scan_code = (*info).scanCode;
      let key = record_key_event(
        &mut context.held_keys,
        scan_code,
        msg_type
      );
      if let Some(key) = key {
        let handler = &context.key_event_handler;
        let action = handler(key, &context.held_keys);
        if let Some(action) = action {
          let result = context.action_sender.send(action);
          if let Err(error) = result {
            println!("ERROR failed to send action to channel: {error}");
          }
          handled = true;
        }
      }
    }
  }
  println!("HOOK h:{handled} {}μs ", start.elapsed().as_micros());
  match handled {
    true => 1,
    false => CallNextHookEx(0, code, w_param, l_param),
  }
}

fn record_key_event(
  held_keys: &mut HashSet<Key>,
  scan_code: u32,
  msg_type: u32
) -> Option<Key> {
  let key = Key::from_scan_code(scan_code)?;
  let is_up = msg_type == WM_KEYUP || msg_type == WM_SYSKEYUP;
  let modified = if is_up {
    held_keys.remove(&key)
  } else {
    held_keys.insert(key)
  };
  if !modified {
    None
  } else {
    Some(key)
  }
}
