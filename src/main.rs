use std::collections::HashSet;
use anyhow::Result;
use window_manager::keys::Key;
use window_manager::colors::Color;
use window_manager::window::{Window, Rect};
use window_manager::desktop;
use window_manager::hotkey;
use window_manager::active_border;

pub const GAP_SIZE: i32 = 20;
pub const BORDER_SIZE: i32 = 8;
pub const BORDER_RADIUS: i32 = 12;
pub const BORDER_COLOR: Color = Color::new(25, 120, 20);

fn main() {
  let active_window_settings = active_border::Settings{
    border_color: BORDER_COLOR,
    border_radius: BORDER_RADIUS,
    border_size: BORDER_SIZE,
  };
  window_manager::run(
    active_window_settings,
    Box::new(handle_key),
  );
}

macro_rules! map {
  ($state: expr, $key:expr, $down:expr, $call:expr) => {
    if $state.0 == $key && *$state.1 == HashSet::from($down) {
      return Some((stringify!($call).into(), Box::new(move || { $call })))
    }
  }
}

fn handle_key(key: Key, held: &HashSet<Key>) -> Option<hotkey::HotkeyAction> {
    use Key::*;
    use Direction::*;
    let s = (key, held);
    map!(s, Backtick,  [Win, Shf],       print_windows());
    map!(s, Backtick,  [Win],            terminal());
    map!(s, H,         [H, Win],         focus(Left));
    map!(s, J,         [J, Win],         focus(Down));
    map!(s, K,         [K, Win],         focus(Up));
    map!(s, L,         [L, Win],         focus(Right));
    map!(s, SemiColon, [SemiColon, Win], focus_under());
    map!(s, H,         [H, Win, Shf],    push(Left));
    map!(s, J,         [J, Win, Shf],    push(Down));
    map!(s, K,         [K, Win, Shf],    push(Up));
    map!(s, L,         [L, Win, Shf],    push(Right));
    map!(s, Y,         [Y, Win, Shf],    push_fullscreen());
    map!(s, U,         [U, Win, Shf],    push_maximize());
    map!(s, I,         [I, Win, Shf],    push_center());
    map!(s, Num(1),    [Num(1), Win],    desktop(0));
    map!(s, Num(2),    [Num(2), Win],    desktop(1));
    map!(s, Num(3),    [Num(3), Win],    desktop(2));
    map!(s, Num(4),    [Num(4), Win],    desktop(3));
    map!(s, Num(5),    [Num(5), Win],    desktop(4));
    map!(s, Num(6),    [Num(6), Win],    desktop(5));
    map!(s, Num(7),    [Num(7), Win],    desktop(6));
    map!(s, Num(8),    [Num(8), Win],    desktop(7));
    map!(s, Num(9),    [Num(9), Win],    desktop(8));
    map!(s, Num(0),    [Num(0), Win],    desktop(9));
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

fn terminal() -> Result<()> {
  #[derive(serde::Serialize)]
  struct Request<'a> {
    name: String,
    command: &'a str,
  }
  let distro = std::fs::read_to_string("C:\\projects\\devenv-wsl\\distro")?;
  let api = std::fs::read_to_string("C:\\projects\\devenv-wsl\\api")?;
  let route = format!("{api}/new-window");
  let body = Request {
    name: distro,
    command: "cd /root && /bin/bash",
  };
  let client = reqwest::blocking::Client::new();
  client.post(route).json(&body).send()?;
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
  let g = GAP_SIZE;
  let hg = GAP_SIZE / 2;
  let ghg = GAP_SIZE + hg;
  let (w, h) = a.resolution()?;
  let rect = match direction {
    Direction::Left =>  Rect {
      x: g,
      y: a.rect.y,
      w: w / 2 - ghg,
      h: a.rect.h
    },
    Direction::Right => Rect {
      x: w / 2 + hg,
      y: a.rect.y,
      w: w / 2 - ghg,
      h: a.rect.h
    },
    Direction::Down =>  Rect {
      x: a.rect.x,
      y: h / 2 + hg,
      w: a.rect.w,
      h: h / 2 - ghg
    },
    Direction::Up =>    Rect {
      x: a.rect.x,
      y: g,
      w: a.rect.w,
      h: h / 2 - ghg
    },
  };
  a.set_rect(rect)?;
  Ok(())
}

fn push_fullscreen() -> Result<()> {
  let a = match Window::active()? {
    Some(a) => a,
    _ => return Ok(()),
  };
  let (w, h) = a.resolution()?;
  let rect = Rect {
    x: 0,
    y: 0,
    w,
    h,
  };
  a.set_rect(rect)?;
  Ok(())
}

fn push_maximize() -> Result<()> {
  let a = match Window::active()? {
    Some(a) => a,
    _ => return Ok(()),
  };
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
