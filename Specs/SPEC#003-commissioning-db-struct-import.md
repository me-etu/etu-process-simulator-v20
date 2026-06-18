# SPEC#003 Commissioning DB Struct Import

## Summary

Extend the Commissioning DB import workflow so TIA Portal `.db` source files can include inline nested `Struct ... END_STRUCT` declarations. The importer should flatten supported primitive struct leaves into generated commissioning JSON, display them in the existing Commissioning DB view, and allow immediate PLCSIM writes using exact API-visible symbols such as `dbIBN.BL170.rawWaterSourceTank.tankLevelRaw`.

The realistic target fixture is `references/dbIBN_withStructs.db`.

## Problem

SPEC#002 added useful commissioning DB import for top-level `Bool` and `Real` variables. Real commissioning DBs often group values inside inline structs, especially per-unit or per-subsystem exchange data. The current importer is line-oriented and only understands top-level declarations, so it would either skip struct declarations or incorrectly flatten nested members as if they were top-level DB members.

Operators need nested struct leaves to appear in the Commissioning DB UI with their full context and write to the exact PLCSIM symbol path.

## Goals

- Parse inline `Struct ... END_STRUCT` declarations inside the first DB `VAR ... END_VAR` block.
- Support nested structs at least two levels deep, including the shape in `references/dbIBN_withStructs.db`.
- Import primitive struct leaves declared as `Bool`, `Real`, and `Int`.
- Flatten imported symbols using the full DB/member path, for example `dbIBN.BL170.rawWaterSourceTank.tankId`.
- Keep the current symbolic-only commissioning DB write path and marker-fallback separation.
- Keep generated config backward-compatible with existing top-level `Bool` and `Real` imports.
- Report unsupported declarations without aborting valid imports.

## Non-Goals

- Do not support external UDT definitions in this spec.
- Do not support arrays, optimized offsets, `VAR_TEMP`, `VAR_OUTPUT`, comments-as-metadata, or default/start values.
- Do not infer types from names.
- Do not redesign the Commissioning DB UI beyond showing qualified nested names clearly.
- Do not generate C# code from DB imports.
- Do not commit project-specific DB exports unless the user explicitly asks; `.db` fixtures may remain local/project-specific.

## Current Behavior

- `CommissioningDbConfigLoader.Parse(...)` reads the DB name from `DATA_BLOCK "..."`.
- It parses only simple `name { attributes } : Bool;` and `name { attributes } : Real;` declarations inside the first top-level `VAR ... END_VAR` block.
- It does not understand `Struct`, `END_STRUCT`, or nested declaration paths.
- `Int` is not currently imported for commissioning DB variables.
- Runtime writes use exact PLCSIM API-visible tag names such as `dbIBN.testbool1`.
- Before commissioning writes, `PLCInstance` refreshes DB tags with `UpdateTagList(..., "\"dbName\"")`.
- `TEST IO` now runs asynchronously with progress and includes commissioning Bool/Real diagnostics.

## References

- `references/AGENTS.md`
- `skills.md`
- `references/SIMULATOR_HANDOFF.md`
- `references/dbIBN_withStructs.db`
- `references/s7-plcsim_advanced_function_manual_API_de-DE.pdf`
- `Specs/SPEC#002-commissioning-db-import.md`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/Models/CommissioningDbConfig.cs`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/Models/PLCInstance.cs`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/PlcIo.cs`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/ViewModels/MainWindowViewModel.cs`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/Views/MainWindow.xaml`

## Proposed Architecture

Replace the current line-only commissioning DB parser with a small deterministic stack-based parser:

- Strip trailing `//` comments before matching declarations.
- Enter parsing at the first top-level `VAR` and stop only at the matching top-level `END_VAR`.
- Push a struct member name when matching `name { attributes } : Struct`.
- Pop one struct path segment when matching `END_STRUCT;`.
- Import supported primitive leaves by joining the current struct path with the leaf name.

For `references/dbIBN_withStructs.db`, generate examples like:

```json
{
  "uiId": "dbIBN_BL170_rawWaterSourceTank_tankLevelRaw",
  "displayName": "BL170.rawWaterSourceTank.tankLevelRaw",
  "plcTag": "dbIBN.BL170.rawWaterSourceTank.tankLevelRaw",
  "dataType": "Real",
  "defaultValue": 0.0
}
```

Type handling:

- `Bool` continues to use checkbox/toggle behavior and `WriteBool`/`ReadBool`.
- `Real` continues to use invariant-culture text input and `WriteFloat`/`ReadFloat`.
- `Int` maps to Siemens `Int16`, uses numeric text input, and calls new or existing `PlcIo`/`PLCInstance` Int16 commissioning helpers.

UI behavior:

- Keep the existing Commissioning DB panel.
- Display nested variables with qualified `displayName` values such as `BL170.rawWaterSourceTank.tankLevelRaw`.
- Use text input for `Real` and `Int`, checkbox for `Bool`.

Diagnostics:

- Extend imported diagnostics to include commissioning `Int` tags as Int16 checks.
- Preserve existing Bool/Real diagnostics and progress reporting.

## FRs

- FR1: Import top-level `Bool`, `Real`, and `Int` DB variables.
- FR2: Parse inline `Struct` declarations inside the first DB `VAR ... END_VAR` block.
- FR3: Support nested inline structs in `references/dbIBN_withStructs.db`, including `BL170.rawWaterSourceTank`.
- FR4: Generate `plcTag` using the full flattened path, for example `dbIBN.BL170.street1Running`.
- FR5: Generate WPF-safe `uiId` values from the full flattened path without modifying `plcTag`.
- FR6: Use qualified `displayName` values for nested leaves so operators can see context.
- FR7: Strip trailing `//` comments before parsing declarations.
- FR8: Ignore declaration attributes such as `{ S7_SetPoint := 'True' }`.
- FR9: Import only supported primitive leaves: `Bool`, `Real`, and `Int`.
- FR10: Report unsupported declarations, unmatched `END_STRUCT`, and unclosed structs clearly.
- FR11: Add commissioning `Int` write/read support through PLCSIM Int16 APIs.
- FR12: Include imported commissioning `Int` tags in `TEST IO` diagnostics with progress.
- FR13: Keep existing top-level Bool/Real import behavior working.
- FR14: Keep plant IO config loading and marker fallbacks unchanged.

## NFRs

- Keep implementation compatible with .NET Framework 4.8 WPF.
- Do not add external dependencies.
- Parser behavior must be deterministic and independent of file enumeration order.
- Numeric parsing and formatting for Real and Int must use invariant culture.
- Import errors must be understandable to a commissioning engineer.
- Do not flood status logs during repeated PLCSIM write failures.

## Validation And Testing

Use the targeted build command:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "Simulator Code\Vacudest\CoSimulationPlcSimAdv\CoSimulationPlcSimAdv.csproj" /t:Build /p:Configuration=Debug /p:Platform=x64
```

Parser validation:

- Imports existing top-level `Bool` and `Real` variables from `references/dbIBN_withStructs.db`.
- Imports `BL170.street1Running` and other direct `BL170` Bool leaves.
- Imports nested `BL170.rawWaterSourceTank.tankId` as `Int`.
- Imports nested `BL170.rawWaterSourceTank.tankLevelRaw` as `Real`.
- Strips the inline comment on `rawWaterSourceTank : Struct   // ...`.
- Reports unsupported arrays, UDT references, malformed declarations, unmatched `END_STRUCT`, and unclosed structs without crashing.
- Fails with a clear message if no DB name is found or no supported variables are imported.

Runtime/UI validation:

- Imported nested Bool leaves appear as checkboxes and write to exact PLCSIM paths.
- Imported nested Real leaves appear as numeric text inputs and write through Float/Real calls.
- Imported nested Int leaves appear as numeric text inputs and write through Int16 calls.
- `TEST IO` progress includes commissioning Bool, Real, and Int sections.
- Existing `dbIBN.db`-style top-level imports still work.

## Plan

1. Create a focused branch from `main`, for example `commissioning-db-struct-import`.
2. Refactor `CommissioningDbConfigLoader.Parse(...)` into a stack-based parser with helper methods for comment stripping, declaration matching, and path joining.
3. Add `Int` as a supported commissioning data type and normalize it consistently as `Int16` or `Int` in generated config; choose one representation and use it everywhere.
4. Add commissioning Int16 read/write helpers in `PlcIo` and `PLCInstance`.
5. Update the Commissioning DB UI template so Int variables use text input like Real variables.
6. Update `MainWindowViewModel.WriteCommissioningReal(...)` or split numeric write handlers so Real and Int commit to the correct PLCSIM API.
7. Extend diagnostic tag collection and `TEST IO` report/progress to include commissioning Int variables.
8. Validate parser output with `references/dbIBN_withStructs.db`.
9. Run targeted MSBuild and manual PLCSIM smoke checks.
10. Update `CHANGELOG.md` and `skills.md` with struct parser and Int commissioning lessons.

## Acceptance Criteria

- Importing `references/dbIBN_withStructs.db` generates config entries for top-level and nested supported leaves.
- Nested struct PLC tags use full API-visible paths such as `dbIBN.BL170.rawWaterSourceTank.tankLevelRaw`.
- `Bool`, `Real`, and `Int` leaves render with the correct editing controls.
- Bool edits write immediately to PLCSIM.
- Real edits write through Float/Real PLCSIM calls.
- Int edits write through Int16 PLCSIM calls.
- `TEST IO` covers imported commissioning Bool, Real, and Int tags and keeps progress visible.
- Unsupported declarations are reported clearly and do not abort valid imports.
- Existing SPEC#002 top-level import behavior remains intact.
- The simulator project builds successfully with the targeted MSBuild command.

## Open Questions

- Should generated config store Siemens `Int` as `"Int"` or normalized runtime type `"Int16"`?
- Should nested display names include the DB name, or only the member path below the DB such as `BL170.rawWaterSourceTank.tankId`?
- Should project-specific DB exports like `references/dbIBN_withStructs.db` remain untracked like `references/dbIBN.db`, or should a sanitized fixture be tracked for parser validation?
- Should Int inputs enforce `-32768..32767` in the UI before write, or rely on parse/write failure handling?

## Git Specifics

- Current spec branch: `main` at spec creation time.
- Suggested implementation branch: `commissioning-db-struct-import`.
- Keep this spec as a focused docs change.
- Do not commit `.vs/`, `bin/`, or `obj`.
- Treat real customer/project DB exports as project-specific unless the user explicitly approves tracking a sanitized fixture.
- Suggested spec commit message: `Add SPEC003 commissioning DB struct import spec`.
- Suggested implementation commit message: `Add SPEC003 commissioning DB struct import`.
