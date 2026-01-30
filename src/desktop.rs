use anyhow::{anyhow, Result};
use winvd::*;
use crate::{window::*};

pub fn switch(n: u32) -> Result<()> {
  let count = get_desktop_count()
    .map_err(|e| anyhow!("error getting desktop count: {e:?}"))?;
  if n < count {
    switch_desktop(n)
      .map_err(|e| anyhow!("error switching desktop: {e:?}"))?;
    let windows = Window::all()?;
    if let Some(window) = windows.first() {
      window.set_active()?;
    }
  }
  Ok(())
}
