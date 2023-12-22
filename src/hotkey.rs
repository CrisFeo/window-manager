use std::borrow::Cow;
use std::collections::HashSet;
use std::thread::{JoinHandle, spawn};
use std::sync::{Mutex, MutexGuard};
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

static CONTEXT: Mutex<Option<Context>> = Mutex::new(None);

pub fn setup(key_event_handler: KeyEventHandler) -> Result<JoinHandle<()>> {
  let (tx, rx) = channel();
  let context = Context{
    key_event_handler,
    held_keys: HashSet::new(),
    action_sender: tx,
  };
  *CONTEXT.lock().expect("shouldn't fail to retrieve context during setup") = Some(context);
  win_err!(SetWindowsHookExW(WH_KEYBOARD_LL, Some(key_hook), 0, 0))?;
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

fn get_context() -> MutexGuard<'static, Option<Context>> {
  match CONTEXT.lock() {
    Ok(context) => context,
    Err(_) => panic!("could not obtain context from mutex"),
  }
}

unsafe extern "system" fn key_hook(code: i32, w_param: WPARAM, l_param: LPARAM) -> LRESULT {
  let msg_type = w_param as u32;
  let info = l_param as *mut KBDLLHOOKSTRUCT;
  if code >= 0 && (*info).flags & LLKHF_INJECTED == 0 {
    let mut context = get_context();
    if let Some(context) = context.as_mut() {
      let is_up = msg_type == WM_KEYUP || msg_type == WM_SYSKEYUP;
      let key = Key::from_scan_code((*info).scanCode);
      let modified = if is_up {
        context.held_keys.remove(&key)
      } else {
        context.held_keys.insert(key)
      };
      if modified {
        let handler = &context.key_event_handler;
        let action = handler(key, &context.held_keys);
        if let Some(action) = action {
          let result = context.action_sender.send(action);
          if let Err(error) = result {
            println!("ERROR failed to send action to channel: {error}");
          }
          return 1;
        }
      }
    }
  }
  CallNextHookEx(0, code, w_param, l_param)
}
