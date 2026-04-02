# SysHiberSwitch

A tiny Windows floating utility that keeps the machine awake with one simple switch.

When enabled, it prevents both:

- display sleep
- system sleep

When disabled or exited, Windows returns to the normal power-plan behavior.

## Build

```powershell
.\build.ps1
```

## Run

Start `bin\SysHiberSwitch.exe`, then use the floating panel:

- `开启`: prevent display sleep and system sleep together
- `关闭`: restore normal power-plan behavior
- `退出`: close the app and restore normal behavior

## Package

```powershell
.\build-release.ps1
```

This creates a portable package under `release/`.
