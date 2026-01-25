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
      11 => Some(Num(0)),
      n if (2..=10).contains(&n) => Some(Num(n - 1)),
      n if (0x3A..=0x40).contains(&n) => None, // undefined
      0xE8 => None, // undefined
      n => Some(Code(n)),
    }
  }
}
