# Commissioning DB Import

## Summary

Add a simulator feature that imports a TIA Portal DB source file, extracts top-level commissioning `Bool` and `Real` variables, stores them in generated JSON config, and exposes them in a dedicated Commissioning DB UI view for immediate PLCSIM manipulation.

The v1 target source format is a `.db` text export like `references/dbIBN.db`. Generated PLC symbols must use the exact DB/member path expected by PLCSIM, for example `dbIBN.testbool1`.

## Problem

The simulator can currently work comfortably with memory-style IO and configured `IN_*`, `CTRL_*`, and feedback tags. Commissioning values in a standard DB are not importable as a first-class workflow, so adding or changing many DB variables requires manual UI/config work and makes commissioning slower than marker-backed simulation.

Operators need a simple way to import a commissioning DB, see its supported values in the simulator, and manipulate Bool and Real entries directly from the UI.

## Goals

- Import simple TIA Portal `.db` source files that contain top-level `VAR ... END_VAR` declarations.
- Support top-level `Bool` and `Real` variables in v1.
- Generate persistent JSON config under `Simulator Code/Vacudest/CoSimulationPlcSimAdv/Configs/`.
- Add a dedicated Commissioning DB view separate from plant IO unit panels.
- Allow immediate writes to PLCSIM when a Bool toggle changes or a Real value is committed.
- Include imported DB symbols in diagnostics/tag audit.
- Keep PLC symbol spelling exact and separate from WPF-safe UI IDs.

## Non-Goals

- Do not parse arrays, structs, UDTs, nested declarations, or optimized DB layout offsets in v1.
- Do not infer variable types from names.
- Do not add marker fallback behavior for commissioning DB variables.
- Do not generate C# code from the DB import.
- Do not replace the existing `DeviceUi_*.json` plant IO configuration path.
- Do not redesign the full WPF simulator UI or convert it fully to MVVM.

## Current Behavior

- `DeviceUiConfigLoader` loads plant IO config files matching `DeviceUi_*.json`.
- Config-driven plant IO supports analog inputs, digital inputs, valves, actuators, and optional marker fallbacks.
- Existing analog runtime behavior is `Int16`-centric and writes scaled raw values through `PlcIo.TryWriteInt16`.
- Existing digital runtime behavior writes `Bool` values through `PlcIo.TryWriteBool`.
- `DeviceUiTemplate.json` already notes that DB variables can be represented as symbolic paths such as `db5000TA1_VLT.Ready`, but there is no importer or dedicated commissioning DB UI.
- `RunDiagnosticIoTest` audits hardcoded and config-driven Bool/Int16 tags, but not imported Real DB symbols.
- `references/dbIBN.db` is the sample TIA DB source. Its `testreal1` through `testreal11` declarations are intended to be `Real`; their current `Bool` declarations should be treated as a sample typo, not as an importer rule.

## References

- `references/AGENTS.md`
- `skills.md`
- `references/SIMULATOR_HANDOFF.md`
- `references/dbIBN.db`
- `Specs/unit-device-id-namespacing.md`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/Configs/DeviceUiTemplate.json`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/Models/DeviceUiConfig.cs`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/PlcIo.cs`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/ViewModels/MainWindowViewModel.cs`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/Views/MainWindow.xaml.cs`

## Proposed Architecture

Introduce a commissioning DB path that is separate from `DeviceUi_*.json` plant IO config:

- `CommissioningDbImporter` parses a selected `.db` source file.
- `CommissioningDbConfig` represents generated JSON config.
- `CommissioningDbConfigLoader` loads generated config files, for example `CommissioningDb_dbIBN.json`.
- A dedicated WPF Commissioning DB view displays imported variables.
- Runtime writes use symbolic DB tags only, with no marker fallback.

Suggested generated JSON shape:

```json
{
  "sourceFile": "references/dbIBN.db",
  "sourceImportedAtUtc": "2026-06-17T00:00:00Z",
  "dbName": "dbIBN",
  "variables": [
    {
      "uiId": "dbIBN_testbool1",
      "displayName": "testbool1",
      "plcTag": "dbIBN.testbool1",
      "dataType": "Bool"
    },
    {
      "uiId": "dbIBN_testreal1",
      "displayName": "testreal1",
      "plcTag": "dbIBN.testreal1",
      "dataType": "Real",
      "defaultValue": 0.0
    }
  ]
}
```

Parsing rules:

- Read `DATA_BLOCK "dbName"` and use the unquoted name in generated tags.
- Parse only lines inside the first top-level `VAR ... END_VAR` block.
- Accept `name { attributes } : Bool;` and `name { attributes } : Real;`.
- Ignore declaration attributes such as `{ S7_SetPoint := 'True' }`.
- Preserve the declared type; do not use name heuristics.
- Skip unsupported declaration lines with an import report entry.
- Fail the import if no DB name is found or if no supported variables are imported.

Runtime and UI rules:

- Bool rows use a toggle button or checkbox and write immediately through `PlcIo.TryWriteBool`.
- Real rows use a numeric text input and write on Enter or focus loss through a new `PlcIo.TryWriteReal`/`TryReadReal` wrapper around PLCSIM `WriteFloat`/`ReadFloat`.
- UI IDs must be WPF-safe and generated from DB/member names without changing `plcTag`.
- The Commissioning DB view should be reachable from the main simulator window as a separate tab or panel from unit-specific plant IO.
- All PLCSIM API calls must continue to run through the existing safe runtime pattern; do not introduce random direct API calls from import parsing or UI event handlers.

## FRs

- FR1: Add a UI action to import a TIA `.db` source file.
- FR2: Extract the DB name from `DATA_BLOCK "..."`.
- FR3: Extract supported top-level variables from `VAR ... END_VAR`.
- FR4: Import only `Bool` and `Real` variables in v1.
- FR5: Generate `plcTag` values as `{dbName}.{memberName}`, for example `dbIBN.testbool1`.
- FR6: Generate WPF-safe `uiId` values without modifying `plcTag`.
- FR7: Persist imported variables to generated JSON config under the simulator `Configs` folder.
- FR8: Load generated commissioning DB config on startup.
- FR9: Show imported variables in a dedicated Commissioning DB view.
- FR10: Write Bool edits immediately to PLCSIM.
- FR11: Write Real edits immediately when the value is committed.
- FR12: Include imported Bool and Real tags in diagnostic/tag audit output.
- FR13: Report skipped unsupported declarations after import.
- FR14: Keep existing plant IO config loading and marker fallbacks unchanged.

## NFRs

- Keep implementation surgical and compatible with the current .NET Framework 4.8 WPF app.
- Do not require network access or new external dependencies.
- Import errors must be understandable to a commissioning engineer.
- Parsing must be deterministic and not depend on file system enumeration order.
- Numeric Real parsing and formatting should use invariant culture.
- Avoid flooding the status log; use the existing log-once style for repeated PLCSIM write/read failures.

## Validation And Testing

Use this targeted build command:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "Simulator Code\Vacudest\CoSimulationPlcSimAdv\CoSimulationPlcSimAdv.csproj" /t:Build /p:Configuration=Debug /p:Platform=x64
```

Parser validation:

- Imports `references/dbIBN.db` with DB name `dbIBN`.
- Imports declared `Bool` variables.
- Imports declared `Real` variables when the sample declarations are corrected to `Real`.
- Ignores declaration attributes.
- Reports unsupported arrays, structs, UDTs, and malformed declaration lines without crashing.
- Fails with a clear message when no DB name or no supported variables are found.

Runtime/UI validation:

- Imported Bool variable `dbIBN.testbool1` appears in the Commissioning DB view and writes immediately when toggled.
- Imported Real variable `dbIBN.testreal1` appears in the Commissioning DB view and writes through PLCSIM Float/Real calls when committed.
- Imported Bool and Real tags appear in diagnostic/tag audit output.
- Existing `DeviceUi_BL170.json` UI generation and plant IO behavior continue to work.

## Plan

1. Create a focused branch from `main`, for example `commissioning-db-import`.
2. Add commissioning DB model classes for config and variable definitions.
3. Add a `.db` parser for simple top-level Bool/Real declarations.
4. Add generated JSON save/load support under `Configs`.
5. Add Real read/write helpers in `PlcIo`.
6. Add commissioning DB runtime write handling using existing safe PLCSIM interaction patterns.
7. Add a dedicated Commissioning DB tab or panel in the WPF main window.
8. Add an Import DB command/button that opens a file picker, parses the selected `.db`, writes generated JSON, reloads the commissioning view, and reports skipped lines.
9. Extend diagnostic/tag audit to include imported Bool and Real variables.
10. Validate with the targeted MSBuild command and manual PLCSIM smoke checks.
11. Update `CHANGELOG.md`, `skills.md`, and README/setup notes only if implementation changes behavior, workflow, or reusable project learnings.

## Acceptance Criteria

- A user can import a TIA `.db` source file from the simulator UI.
- Supported top-level Bool and Real DB variables are persisted to generated JSON config.
- Imported variables reload after simulator restart.
- Imported variables are displayed in a dedicated Commissioning DB view.
- Bool edits write immediately to the exact generated DB symbol.
- Real edits write immediately through Float/Real PLCSIM calls.
- Unsupported declarations are reported clearly and do not abort valid imports.
- Diagnostic/tag audit covers imported Bool and Real DB symbols.
- Existing memory-marker-backed plant IO behavior remains unchanged.
- The simulator project builds successfully with the targeted MSBuild command.

## Open Questions

- Should generated commissioning JSON overwrite an existing file for the same DB name automatically, or should the UI ask before replacing it?
- Should imported Real variables support min/max/default metadata from comments or attributes in a later version?
- Should multiple commissioning DB configs be shown together in one view or separated by DB tabs if more than one DB is imported?
- Should the implementation correct `references/dbIBN.db` so `testreal1` through `testreal11` are declared as `Real`, or should a separate corrected fixture be added?

## Git Specifics

- Current spec branch: `commissioning-db-import-spec`.
- Keep this spec as one focused docs commit.
- Do not modify `references/dbIBN.db` while creating this spec; it is currently an untracked sample file.
- Do not commit `.vs/`, `bin/`, or `obj/`.
- Suggested implementation branch after this spec: `commissioning-db-import`.
- Suggested spec commit message: `Add commissioning DB import spec`.
- Suggested implementation commit message: `Add commissioning DB import workflow`.
