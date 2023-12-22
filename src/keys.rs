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
  pub fn from_scan_code(scan_code: u32) -> Self {
    use Key::*;
    match scan_code {
      91 => Win,
      42 => Shf,
      29 => Ctl,
      56 => Alt,
      35 => H,
      36 => J,
      37 => K,
      38 => L,
      20 => T,
      21 => Y,
      22 => U,
      23 => I,
      39 => SemiColon,
      41 => Backtick,
      11 => Num(0),
      n if (2..=10).contains(&n) => Num(n - 1),
      n => Code(n),
    }
  }
}
