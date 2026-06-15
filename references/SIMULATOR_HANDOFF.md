# PLC Simulator Handoff Notes

These notes summarize the important lessons from debugging and improving the PLCSIM Advanced simulator so another Codex session can pick up the project quickly.

## Current Context

The simulator is a WPF/C# application using the Siemens PLCSIM Advanced API. It starts PLCSIM Advanced, creates or attaches to an instance, downloads/runs from TIA Portal, and writes simulated IO values to the PLC instance.

The most important conclusion from debugging:

- The PLCSIM connection can be valid while symbolic IO writes still fail because of stale API interfaces, missing tag list refresh, wrong symbol names, or UI control-name mapping bugs.
- Direct diagnostic write/read tests are extremely useful because they separate "PLCSIM API can write the tag" from "the simulator UI path is wired correctly."

## Known Good Runtime Pattern

Use one dedicated worker thread for all PLCSIM Advanced API calls.

Reason:

- The Siemens API is sensitive to threading.
- Running read/write calls on the WPF UI thread can freeze the window.
- Calling the API from multiple random background threads can create stale or inconsistent behavior.

Preferred structure:

- `PLCInstance` owns a single worker thread.
- UI commands enqueue work to that worker.
- The cyclic simulation loop also runs on that same worker.
- UI updates are marshaled back to WPF via `Dispatcher`.

## Stale Interface Problem

Observed symptom:

- IO simulation works for a while.
- After idle time, UI controls stop affecting the PLC.
- Pressing `TEST IO` makes everything work again.

Root cause:

- `TEST IO` forced `ConnectOrReconnectInterface()` and `UpdateTagList(...)`.
- The normal cyclic loop was sometimes using a stale `IInstance` or stale symbol/tag list.

Recommended fix:

- Reconnect/recreate the PLCSIM API interface at important lifecycle points:
  - after instance creation/attach
  - after `PowerOn`
  - after `Run`
  - after hardware/config update events
  - periodically during long-running cyclic operation
  - immediately after cyclic read/write failure

Important detail:

- If reconnect replaces the `IInstance`, any event subscriptions on the old `IInstance` are lost.
- Do not let the ViewModel subscribe directly to `virtualController.instance.OnOperatingStateChanged`.
- Instead expose a stable event on `PLCInstance`, for example:

```csharp
public event Action<EOperatingState> OperatingStateChanged;
```

Then raise that event whenever the current instance state changes or after reconnect.

## Status Field Issue

Observed symptom:

- `StatusPLCInstance` stopped updating.

Root cause:

- The UI subscribed to `OnOperatingStateChanged` on one `IInstance`.
- Reconnect created a new `IInstance`.
- The UI remained subscribed to the old instance.

Recommended pattern:

- `MainWindowViewModel` subscribes to `PLCInstance.OperatingStateChanged`.
- `PLCInstance` internally subscribes/unsubscribes to the current `IInstance`.
- `PLCInstance` raises the stable event after reconnect and state changes.

## Symbol Names vs UI Names

Many bugs came from confusing PLC tag names with WPF control names.

Examples:

- PLC tag: `E_NotAus_Ok`
- Wrong code tag: `E_NotAus_OK`
- WPF control: `NotAus_Ok_ActValueButton`

Another example:

- PLC tag: `IN_FISA3152_41`
- WPF control: `FISA3152_41_SetpointButton`

Important rule:

- The exact PLC tag name must be used for `ReadBool`, `WriteBool`, `ReadInt16`, `WriteInt16`, etc.
- A separate UI-safe ID should be used for WPF control lookup.
- Do not derive PLC names from UI names.
- Do not assume case-insensitivity; Siemens symbolic tags are effectively case-sensitive for this use.

Good helper idea:

```csharp
private static string ToUiId(string plcTag)
{
    return plcTag
        .Replace("IN_", "")
        .Replace("E_", "")
        .Replace("-", "_");
}
```

But only use this for UI lookup, not for PLCSIM reads/writes.

## Digital IO Lessons

Digital inputs failed even though tags existed because button lookup stripped only `IN_`, not `E_`.

Recommended digital behavior:

- Store exact PLC tag in the `Digital` object.
- Store a separate UI ID for control lookup.
- On click, update the internal desired value.
- The cyclic loop writes desired value to the exact PLC tag.
- Log write failures once per tag.

Avoid:

- Empty `catch { }` blocks around API calls.
- Defaulting all digital inputs to `true`.

## Analog IO Lessons

Analog simulation had a similar UI mapping problem.

If the PLC tag is:

```text
IN_FISA3152_41
```

The WPF controls may be:

```text
FISA3152_41_SetpointEdit
FISA3152_41_SetpointButton
FISA3152_41_ActValueButton
```

So analog control lookup must normalize the UI name separately from the PLC tag.

Recommended analog model:

- Exact `TagName`
- UI-safe `Id`
- `Min`
- `Max`
- `DefaultValue`
- `Setpoint`
- `SimEnabled`
- `Quality/Error` state if applicable

## Valve Simulation Lessons

The old valve code assumed tags could be generated from a valve name:

```text
CTRL_<ValveName>
FB_OPN_<ValveName>
FB_CLS_<ValveName>
FB_OPN_<ValveName>_QB
FB_CLS_<ValveName>_QB
```

That works only if the PLC project follows the exact convention.

Problem cases found:

- Valve names used `HV3150-81` in code but `HV3150_81` in TIA.
- Feedback tags did not exist initially.
- Manual feedback simulation returned early if `CTRL_*` could not be read.
- `OPEN` / `CLOSE` buttons wrote inverted values.

Recommended valve behavior:

- Make every valve tag explicit, not guessed.
- Manual feedback simulation should not depend on successfully reading `CTRL_*`.
- Automatic mode can read `CTRL_*` and write feedback after travel time.
- Manual mode should directly force feedback tags.
- Quality bits should be optional.

Suggested valve model:

```csharp
public class ValveDefinition
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string CtrlTag { get; set; }
    public string FbOpenTag { get; set; }
    public string FbCloseTag { get; set; }
    public string FbOpenQualityTag { get; set; }
    public string FbCloseQualityTag { get; set; }
    public bool NormallyClosed { get; set; }
    public int TravelTimeMs { get; set; }
}
```

Modes to support:

- Automatic: read `CtrlTag`, then after travel time write open/close feedback.
- Manual: ignore `CtrlTag`, user forces open/close.
- Fault: feedback does not follow command or quality bit is false.

## New Project IO Pattern From Screenshot

The new project screenshot contains these kinds of IO:

Analog/int inputs:

```text
IN_QC-9-1-8-4
IN_QC-10-1-8-4
IN_PS-6-1-8-4
IN_TC-5-3-1-1
IN_TI-5-10-31-1
IN_TI-5-20-31-1
IN_FI-8-1-31-1
```

Simple actuator feedback pairs:

```text
CTRL_G21
FB_ON_G21
CTRL_E21
FB_ON_E21
```

Valve groups:

```text
CTRL_Q21
FB_OPN_Q21
FB_OPN_Q21_QB
FB_CLS_Q21
FB_CLS_Q21_QB

CTRL_Q31
FB_OPN_Q31
FB_OPN_Q31_QB
FB_CLS_Q31
FB_CLS_Q31_QB

CTRL_Q41
FB_OPN_Q41
...
```

Important naming point:

- Some analog tags contain hyphens.
- Valve tags use underscores.
- Therefore, never normalize the PLC tag itself.
- Normalize only the UI ID.

Example:

```csharp
TagName = "IN_QC-9-1-8-4";  // exact PLC symbol
Id = "IN_QC_9_1_8_4";       // UI-safe identifier
```

## Recommended Reusable Architecture

Move away from hardcoded `new Digital(...)`, `new Analog(...)`, and `new Valve(...)` declarations.

Use a configuration file to define all IOs.

Recommended config formats:

- JSON for richer nested valve definitions.
- CSV if the user wants to maintain the IO list in Excel.

Suggested generic IO definition:

```csharp
public class IoDefinition
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string TagName { get; set; }
    public string Type { get; set; } // Bool, Int16, Float, Valve, Motor, Actuator
    public double Min { get; set; }
    public double Max { get; set; }
    public double DefaultValue { get; set; }
    public string Group { get; set; }
}
```

Suggested JSON shape:

```json
{
  "analogs": [
    {
      "id": "QC_9_1_8_4",
      "displayName": "QC 9-1-8-4",
      "tagName": "IN_QC-9-1-8-4",
      "dataType": "Int16",
      "min": 0,
      "max": 30000,
      "defaultValue": 0
    }
  ],
  "actuators": [
    {
      "id": "G21",
      "displayName": "G21",
      "ctrlTag": "CTRL_G21",
      "feedbackTag": "FB_ON_G21"
    }
  ],
  "valves": [
    {
      "id": "Q21",
      "displayName": "Q21",
      "ctrlTag": "CTRL_Q21",
      "fbOpenTag": "FB_OPN_Q21",
      "fbCloseTag": "FB_CLS_Q21",
      "fbOpenQualityTag": "FB_OPN_Q21_QB",
      "fbCloseQualityTag": "FB_CLS_Q21_QB",
      "normallyClosed": true,
      "travelTimeMs": 4000
    }
  ]
}
```

## Recommended UI Improvements

Best long-term direction:

- Generate the UI from configuration.
- Use bindings and view models instead of `FindName(...)`.
- Keep reusable controls:
  - `DigitalIoControl`
  - `AnalogIoControl`
  - `ValveControl`
  - `ActuatorControl`

For each configured IO:

- Display label.
- Display current desired simulation value.
- Allow manual override.
- Show last write/read status.
- Show tag existence check result.

## Diagnostics To Keep

Keep a `TEST IO` or `Tag Audit` button permanently.

It should:

- Iterate all configured tags.
- Call `UpdateTagList(...)`.
- Try read/write where safe.
- Report:
  - `OK`
  - `DoesNotExist`
  - `AccessDenied`
  - datatype mismatch
  - write failed

This catches:

- typo/case mismatches
- tags missing after download
- stale PLCSIM interface
- wrong datatype assumptions

## Error Handling

Avoid empty catches like:

```csharp
catch { }
```

Use a "log once per tag until it succeeds" pattern to avoid flooding the UI:

```csharp
private readonly HashSet<string> loggedTagFailures = new HashSet<string>();

private void LogTagFailureOnce(string tagName, string message)
{
    if (loggedTagFailures.Add(tagName))
    {
        App?.LogStatus(message);
    }
}
```

When a tag succeeds again, remove it from the failure set.

## Practical Rules For Future Codex Sessions

When IO simulation fails:

1. Verify the exact symbolic tag name with a direct diagnostic write/read.
2. If direct test works but UI does not, inspect UI control mapping.
3. If direct test only works after reconnect, inspect stale `IInstance` and `UpdateTagList`.
4. If valves fail, check whether manual simulation depends on reading a missing `CTRL_*` tag.
5. If status stops updating, check whether the UI is subscribed to a stale `IInstance`.

Preferred fix order:

1. Stabilize PLCSIM API threading and reconnect logic.
2. Add tag audit diagnostics.
3. Separate PLC tag names from UI IDs.
4. Make IO definitions explicit/config-driven.
5. Generate UI from config.

