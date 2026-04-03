# SysHiberSwitch

A tiny Windows floating utility that automatically keeps the machine awake while monitored creative apps are still active.

The current app monitors:

- `Photoshop`
- `Cinema 4D`

The keep-awake rule is:

- if either monitored app is active, keep-awake stays enabled
- if either monitored app is in the idle countdown stage, keep-awake stays enabled
- only when both apps are not running or have fully timed out does Windows return to its normal power-plan behavior

When keep-awake is enabled, the app prevents both:

- display sleep
- system sleep

Cinema 4D uses CPU activity plus GPU Engine utilization to determine whether it is still active. Photoshop currently uses CPU activity only.

## Build

```powershell
.\build.ps1
```

## Run

Start `bin\SysHiberSwitch.exe`, then use the floating panel to view:

- current keep-awake status
- Photoshop status
- Cinema 4D status
- aggregated idle countdown when protection is about to expire
- auto-start toggle
- `Exit` button: close the app and restore normal behavior

The app applies keep-awake automatically based on monitored application activity. It is no longer a manual on/off switch.

## Package

```powershell
.\build-release.ps1
```

This creates a portable package under `release/`.
