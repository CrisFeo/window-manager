# Simple C# Window Manager

Super simple window management for windows. Emphasis on the *simple*,
de-emphasis on the *management*.

## Disabling `win-*` hotkeys

I find that using the windows key as the base hotkey is super convenient but to
fully take advantage of it we need to disable a bunch of the built-in windows
hotkeys by adding values to the registry.

### Disable *most* of the `win-*` hotkeys

**Path:** `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer`

**Type:** `REG_DWORD`

**Name:** `NoWinKeys`

**Value:** `1`


### Disable `win-l` to lock

**Path:** `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\System`

**Type:** `REG_DWORD`

**Name:** `DisableLockWorkstation`

**Value:** `1`

