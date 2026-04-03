# SysHiberSwitch Logic

## Goal

The intended product behavior is:

- keep the machine awake while Photoshop or Cinema 4D is likely still processing work
- allow Windows to return to its normal power-plan behavior only after both applications have been idle for long enough or are no longer running

This document describes the recommended logic model for the app so the implementation can be aligned later.

## Layered Design

The logic should be split into three layers:

1. Detection layer
2. Policy layer
3. Presentation layer

These layers should have clear responsibilities and should not be mixed together.

## 1. Detection Layer

The detection layer answers only one question:

`What is each monitored application doing right now?`

The first monitored applications are:

- `Photoshop`
- `Cinema 4D`

Known application definitions:

- Photoshop
  Process name: `Photoshop`
  GPU activity enabled: `false`

- Cinema 4D
  Process name: `Cinema 4D`
  GPU activity enabled: `true`

Cinema 4D process name has already been verified from Windows PowerShell output, so detection can use:

```csharp
Process.GetProcessesByName("Cinema 4D")
```

Each monitored application uses the same state model, the same 1-second sampling loop, and the same 60-second idle countdown model.
The current implementation does not use exactly the same activity signal for both applications:

- Photoshop uses CPU activity only
- Cinema 4D uses CPU activity plus GPU Engine utilization

That distinction is intentional in the current implementation.

Recommended detector states:

- `NotRunning`
  The application is not running, or no usable main window/process is detected.

- `Active`
  The application shows meaningful activity and should be treated as working.

- `IdleCountdown`
  The application has become idle and the app is counting down toward disabling keep-awake.

- `IdleExpired`
  The application has remained idle through the full countdown and is now considered safe to stop protecting.

### Detector State Transitions

```text
NotRunning
  -> Active                 when the application appears and shows activity

Active
  -> IdleCountdown          when the application becomes idle
  -> NotRunning             when the application exits

IdleCountdown
  -> Active                 when activity resumes
  -> IdleExpired            when countdown reaches zero
  -> NotRunning             when the application exits

IdleExpired
  -> Active                 when activity resumes again
  -> NotRunning             when the application exits
```

### Current Activity Judgment

The current implementation treats an application as usable only when at least one matching process:

- has not exited
- has a non-zero `MainWindowHandle`

For those usable processes, the monitor aggregates:

- total CPU time across all usable matching processes
- matching process IDs for optional GPU lookup

Sampling rules:

- sampling interval: `1 second`
- idle countdown duration: `60 seconds`
- CPU active threshold: `0.02`
- GPU active threshold: `5.0`

Current active decision:

- Photoshop is `Active` when CPU usage is at or above the CPU threshold
- Cinema 4D is `Active` when CPU usage is at or above the CPU threshold, or GPU usage is at or above the GPU threshold
- otherwise the application moves into or remains in `IdleCountdown`
- once the countdown reaches zero, the application becomes `IdleExpired`

This means Cinema 4D has a stricter and more practical activity check than Photoshop in the current codebase, because GPU-heavy work should still block system sleep even when CPU usage is low.

## 2. Policy Layer

The policy layer answers only one question:

`Should the app keep the machine awake right now?`

This layer must not care about UI labels or window controls. It only converts detector state into a keep-awake decision.

Per-application mapping:

- `NotRunning` -> `KeepAwake = false`
- `Active` -> `KeepAwake = true`
- `IdleCountdown` -> `KeepAwake = true`
- `IdleExpired` -> `KeepAwake = false`

Aggregated application policy:

- if Photoshop needs protection, keep-awake stays enabled
- if Cinema 4D needs protection, keep-awake stays enabled
- only when both applications do not need protection can keep-awake be disabled

Equivalent rule:

`KeepAwake = PhotoshopNeedsProtection OR Cinema4DNeedsProtection`

### Policy Meaning

In product terms, the behavior becomes:

- if Photoshop is actively working, keep the machine awake
- if Cinema 4D is actively working, keep the machine awake
- if either application is idle but still inside the shutdown countdown, keep the machine awake
- only stop keeping the machine awake when both applications are gone or have stayed idle long enough

## 3. Presentation Layer

The presentation layer answers only one question:

`How should the current state be shown to the user?`

It should:

- display the current Photoshop status
- display the current Cinema 4D status
- display whether keep-awake is currently enabled
- display a plain-language aggregated reason
- expose the auto-start setting

It should not:

- decide business rules
- directly contain keep-awake policy logic
- reinterpret detector states

## Why The Current Logic Feels Confusing

The main source of confusion is that multiple concerns are currently blended together:

- detector state and policy outcome are mixed
- the name `Busy` carries more than one meaning
- the form both renders UI and performs decision-making

That creates an unclear mental model:

- sometimes a state name describes Photoshop itself
- sometimes it really means the keep-awake decision
- sometimes the UI text is more specific than the actual state model

## Naming Guidance

The recommended state names are intentionally explicit:

- `Active` means the monitored application is doing work
- `IdleCountdown` means the monitored application is idle and still protected during the only countdown stage
- `IdleExpired` means the monitored application has been idle long enough to remove protection

This avoids overloading a name like `Busy` to mean:

- actually processing
- already idle but still inside the shutdown delay

## Recommended Responsibility Split

Suggested ownership by component:

- `ApplicationIdleMonitor`
  Detect one configured application state and expose timing details.

- application monitor configuration
  Provide display name, process name, and whether GPU activity should be included.

- `KeepAwakePolicy`
  Convert each application state into protection-needed flags and aggregate them into one `KeepAwake = true/false` decision.

- `AppState`
  Only apply or clear the Windows execution-state flags.

- `FloatingForm`
  Display aggregated state, show the countdown summary, and handle the auto-start checkbox.

## Product-Level Summary

The intended product rule can be summarized as:

`Keep the machine awake whenever Photoshop or Cinema 4D is active, or when either one has only recently gone idle and is still inside the single shutdown countdown. Release the protection only when both applications are absent or have remained idle long enough.`

In the current implementation, Cinema 4D may remain `Active` because of GPU activity even when CPU usage alone would look idle.

## Implementation Notes For The Separate Dev Thread

When the implementation is updated later, the following principles should hold:

- both Photoshop and Cinema 4D should use the same detector state model
- Cinema 4D detection should use the verified process name `Cinema 4D`
- Cinema 4D activity detection should include GPU Engine utilization as implemented today
- detector state names should reflect application behavior, not UI wording
- keep-awake decisions should be derived in one place
- multi-application keep-awake should be an OR aggregation, not duplicated UI-side branching
- the UI should not contain core business logic
- there should be only one idle countdown stage, not two consecutive 60-second delays
- documentation, labels, and code naming should all describe the same product behavior
