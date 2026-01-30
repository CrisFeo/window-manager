#[derive(Debug, Copy, Clone, PartialEq, Eq, Hash)]
pub enum KeyState {
  Down,
  Up,
  Repeat,
}

#[derive(Debug, Copy, Clone, PartialEq, Eq, Hash)]
pub enum Key {
  Win,
  Shf,
  Ctl,
  Alt,
  H,
  J,
  K,
  L,
  N,
  T,
  Y,
  U,
  I,
  Backtick,
  SemiColon,
  CapsLock,
  Num(u32),
  Code(u32),
}

impl Key {
  pub fn from_scan_code(scan_code: u32) -> Option<Self> {
    use Key::*;
    match scan_code {
      91 => Some(Win),
      42 => Some(Shf),
      29 => Some(Ctl),
      56 => Some(Alt),
      35 => Some(H),
      36 => Some(J),
      37 => Some(K),
      38 => Some(L),
      49 => Some(N),
      20 => Some(T),
      21 => Some(Y),
      22 => Some(U),
      23 => Some(I),
      39 => Some(SemiColon),
      41 => Some(Backtick),
      58 => Some(CapsLock),
      11 => Some(Num(0)),
      n if (2..=10).contains(&n) => Some(Num(n - 1)),
      n if (0x3A..=0x40).contains(&n) => None, // undefined
      0xE8 => None, // undefined
      n => Some(Code(n)),
    }
  }

  pub fn to_scan_code(&self) -> u32 {
    use Key::*;
    match self {
       Win => 91,
       Shf => 42,
       Ctl => 29,
       Alt => 56,
       H => 35,
       J => 36,
       K => 37,
       L => 38,
       N => 49,
       T => 20,
       Y => 21,
       U => 22,
       I => 23,
       SemiColon => 39,
       Backtick => 41,
       CapsLock => 58,
       Num(0) => 11,
       Num(n) => n+1,
       Code(n) => *n,
    }
  }
}
