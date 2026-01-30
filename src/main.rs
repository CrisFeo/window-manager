use std::collections::HashSet;
use std::process::Command;
use anyhow::Result;
use window_manager::keys::{Key, KeyState};
use window_manager::window::{Window, Rect};
use window_manager::desktop;
use window_manager::hotkey;
use window_manager::input;

pub const GAP_SIZE: i32 = 20;

fn main() {
  window_manager::run(
    Box::new(handle_key),
  );
}

macro_rules! remap {
  ($state: expr, $from: expr, $to: expr) => {
    if $state.0 == $from {
      if $state.1 == KeyState::Down || $state.1 == KeyState::Repeat {
      return Some((
        stringify!($from to $to down).into(),
        Box::new(move || { input::send($to, true); Ok(()) }),
      ));
      } else {
        return Some((
          stringify!($from to $to up).into(),
          Box::new(move || { input::send($to, false); Ok(()) }),
        ));
      }
    }
  }
}

macro_rules! map {
  ($state: expr, $key:expr, $held:expr, $call:expr) => {
    if $state.0 == $key {
      let mut held = HashSet::from($held);
      if $state.1 == KeyState::Up {
        if *$state.2 == held {
          return Some((
            stringify!($call).into(),
            Box::new(move || { $call })
          ))
        }
      } else {
        // we need to swallow down and repeat events for our hotkeys
        held.insert($key);
        if *$state.2 == held {
          return Some((
            stringify!(swallowing $call).into(),
            Box::new(move || { Ok(()) })
          ))
        }
      }
    }
  }
}

fn handle_key(key: Key, state: KeyState, held: &HashSet<Key>) -> Option<hotkey::HotkeyAction> {
    use Key::*;
    use Direction::*;
    let s = (key, state, held);
    remap!(s, CapsLock, Ctl);
    map!(s, Backtick,  [Win, Shf], print_windows());
    map!(s, Backtick,  [Win],      terminal("bash --login"));
    map!(s, N,         [Win],      terminal("sh -c '/home/cris/.bin/n'"));
    map!(s, H,         [Win],      focus(Left));
    map!(s, J,         [Win],      focus(Down));
    map!(s, K,         [Win],      focus(Up));
    map!(s, L,         [Win],      focus(Right));
    map!(s, SemiColon, [Win],      focus_under());
    map!(s, H,         [Win, Shf], push(Left));
    map!(s, J,         [Win, Shf], push(Down));
    map!(s, K,         [Win, Shf], push(Up));
    map!(s, L,         [Win, Shf], push(Right));
    map!(s, Y,         [Win, Shf], push_maximize());
    map!(s, U,         [Win, Shf], push_big());
    map!(s, I,         [Win, Shf], push_center());
    map!(s, Num(1),    [Win],      desktop(0));
    map!(s, Num(2),    [Win],      desktop(1));
    map!(s, Num(3),    [Win],      desktop(2));
    map!(s, Num(4),    [Win],      desktop(3));
    map!(s, Num(5),    [Win],      desktop(4));
    map!(s, Num(6),    [Win],      desktop(5));
    map!(s, Num(7),    [Win],      desktop(6));
    map!(s, Num(8),    [Win],      desktop(7));
    map!(s, Num(9),    [Win],      desktop(8));
    map!(s, Num(0),    [Win],      desktop(9));
    None
}

fn print_windows() -> Result<()> {
  let a = match Window::active()? {
    Some(a) => a,
    _ => return Ok(()),
  };
  let (screen_w, screen_h) = a.resolution()?;
  println!("  resolution: {screen_w}x{screen_h}");
  println!(
    "  * {:?}\n    {:?}\n    {:?}\n    {:?}",
    a.title,
    a.class_name,
    a.rect,
    a.offset
  );
  for w in Window::all()? {
    if w.handle == a.handle {
      continue;
    }
    println!(
      "  - {:?}\n    {:?}\n    {:?}\n    {:?}",
      w.title,
      w.class_name,
      w.rect,
      w.offset
    );
  }
  Ok(())
}

fn terminal(command: &str) -> Result<()> {
  let wt = "%LocalAppData%\\Microsoft\\WindowsApps\\wt.exe";
  let wt_args = "--focus --profile Alpine";
  let wsl = "C:\\Windows\\system32\\wsl.exe";
  let wsl_args = "-d Alpine";
  let script = format!("start {wt} {wt_args} {wsl} {wsl_args} -- {command}");
  Command::new("C:\\windows\\system32\\cmd.exe")
    .args([ "/c", &script ])
    .spawn()?;
  Ok(())
}

#[derive(Debug, Copy, Clone)]
pub enum Direction {
  Left,
  Down,
  Up,
  Right,
}

fn focus(direction: Direction) -> Result<()> {
  let a = match Window::active()? {
    Some(a) => a,
    _ => return Ok(()),
  };
  let mut windows = Window::all()?
    .into_iter()
    .filter(|w| w.handle != a.handle)
    .filter(|w| match direction {
      Direction::Left => w.rect.x < a.rect.x,
      Direction::Down => w.rect.y > a.rect.y,
      Direction::Up => w.rect.y < a.rect.y,
      Direction::Right => w.rect.x > a.rect.x,
    })
    .collect::<Vec<Window>>();
  windows.sort_by_key(|w| {
    let x = (a.rect.x - w.rect.x).abs();
    let y = (a.rect.y - w.rect.y).abs();
    (y, x)
  });
  if let Some(window) = windows.first() {
    window.set_active()?;
  }
  Ok(())
}

fn focus_under() -> Result<()> {
  let a = match Window::active()? {
    Some(a) => a,
    _ => return Ok(()),
  };
  let windows = Window::all()
    ?
    .into_iter()
    .filter(|w| w.handle != a.handle)
    .filter(|w| w.rect.x == a.rect.x && w.rect.y == a.rect.y)
    .collect::<Vec<Window>>();
  if let Some(window) = windows.first() {
    window.set_active()?;
  }
  Ok(())
}

fn push(direction: Direction) -> Result<()> {
  let a = match Window::active()? {
    Some(a) => a,
    _ => return Ok(()),
  };
  let was_maximized = a.get_maximized();
  let a = a.ensure_not_maximized()?;
  let g = GAP_SIZE;
  let hg = GAP_SIZE / 2;
  let ghg = GAP_SIZE + hg;
  let (w, h) = a.resolution()?;
  // when we push we only effect the axis being pushed along. In the case of a
  // fullscreen window though we want to treat the axis not being pushed along
  // as a gapped "maximized" window.
  let existing_rect = if was_maximized {
    Rect {
      x: g,
      y: g,
      w: w - 2 * g,
      h: h - 2 * g,
    }
  } else {
    a.rect.clone()
  };
  let rect = match direction {
    Direction::Left =>  Rect {
      x: g,
      y: existing_rect.y,
      w: w / 2 - ghg,
      h: existing_rect.h
    },
    Direction::Right => Rect {
      x: w / 2 + hg,
      y: existing_rect.y,
      w: w / 2 - ghg,
      h: existing_rect.h
    },
    Direction::Down =>  Rect {
      x: existing_rect.x,
      y: h / 2 + hg,
      w: existing_rect.w,
      h: h / 2 - ghg
    },
    Direction::Up =>    Rect {
      x: existing_rect.x,
      y: g,
      w: existing_rect.w,
      h: h / 2 - ghg
    },
  };
  a.set_rect(rect)?;
  Ok(())
}

fn push_maximize() -> Result<()> {
  let a = match Window::active()? {
    Some(a) => a,
    _ => return Ok(()),
  };
  a.set_maximized();
  Ok(())
}

fn push_big() -> Result<()> {
  let a = match Window::active()? {
    Some(a) => a,
    _ => return Ok(()),
  };
  let a = a.ensure_not_maximized()?;
  let (w, h) = a.resolution()?;
  let rect = Rect {
    x: GAP_SIZE,
    y: GAP_SIZE,
    w: w - 2 * GAP_SIZE,
    h: h - 2 * GAP_SIZE,
  };
  a.set_rect(rect)?;
  Ok(())
}

fn push_center() -> Result<()> {
  let a = match Window::active()? {
    Some(a) => a,
    _ => return Ok(()),
  };
  let a = a.ensure_not_maximized()?;
  let (w, h) = a.resolution()?;
  let rect = Rect {
    x: (w - a.rect.w) / 2,
    y: (h - a.rect.h) / 2,
    w: a.rect.w,
    h: a.rect.h,
  };
  a.set_rect(rect)?;
  Ok(())
}

fn desktop(n: u32) -> Result<()> {
  desktop::switch(n)?;
  Ok(())
}
