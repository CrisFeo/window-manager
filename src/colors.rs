use windows_sys::Win32::Foundation::COLORREF;

#[derive(Debug, Copy, Clone)]
pub struct Color(pub COLORREF);

impl Color {
  pub const fn new(r: u8, g: u8, b: u8) -> Self {
    let r = r as u32;
    let g = (g as u32) << 8;
    let b = (b as u32) << 16;
    Color(b | g | r)
  }
}
