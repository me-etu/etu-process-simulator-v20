# Changelog

Internal tracker for meaningful simulator changes, setup notes, and maintenance decisions.

## 2026-06-17 - Repo Workflow And Simulator UI Setup

### Added

- Added BL170 simulator device config and config-driven unit UI generation.
- Added `Specs/` as the tracked home for PRD-style implementation handoff specs.
- Added project-local Codex skills for repo workflows:
  - `create-spec` for drafting handoff-ready specs.
  - `execute-spec` for implementing specs with validation and acceptance-criteria reporting.
  - `warm-up` for read-only session startup context.
  - `update-changelog` for dated, file-specific changelog entries.
  - `git-wrap-up` for final branch/diff/changelog checks, local commit creation, and copy-paste push command output.

### Changed

- Changed the simulator UI organization from a flat device list to a unit sidebar for base/common devices and configured units.
- Changed repository hygiene expectations so `.vs/`, `bin/`, and `obj/` stay out of future commits.
- Changed this changelog from a generic `Unreleased` bullet list to dated entries with file and validation context.

### Files

- `.codex/skills/create-spec/SKILL.md`
- `.codex/skills/execute-spec/SKILL.md`
- `.codex/skills/warm-up/SKILL.md`
- `.codex/skills/update-changelog/SKILL.md`
- `.codex/skills/git-wrap-up/SKILL.md`
- `Specs/unit-device-id-namespacing.md`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/Configs/DeviceUi_BL170.json`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/Configs/DeviceUiTemplate.json`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/Models/DeviceUiConfig.cs`
- `CHANGELOG.md`
- `skills.md`
- `.gitignore`

### Validation

- Local Codex skills are validated with `quick_validate.py`.
- Simulator build validation should be recorded with the implementation entry that completes the active feature branch.

### Notes

- Earlier `Unreleased` bullets were consolidated into this dated entry when the changelog format was improved; exact original dates were not separated.

## 2026-06-17 - Commissioning DB Import Session

### Added

- Added a Commissioning DB import workflow for simple TIA Portal `.db` source exports.
- Added generated `CommissioningDb_*.json` config loading under the simulator `Configs` folder.
- Added a dedicated Commissioning DB simulator view with an `Import DB` action.
- Added imported Bool checkbox writes and Real text-value commits through PLCSIM worker-thread methods.
- Added symbolic-only Bool helpers and Real/Float read/write helpers for commissioning DB variables.
- Added commissioning DB Bool and Real symbols to the `TEST IO` diagnostic audit.
- Added diagnostic logging that reports matching `IInstance.TagInfos` names when commissioning DB writes fail.
- Added inline `TEST IO` progress text and progress bar while the audit is running.

### Changed

- Kept commissioning DB config separate from existing `DeviceUi_*.json` plant IO config and marker fallback behavior.
- Generated commissioning UI rows from imported DB config while preserving exact PLC symbol spelling separately from WPF-safe UI IDs.
- Refreshed PLCSIM tag lists with the DB-specific `UpdateTagList(..., "\"dbName\"")` overload before commissioning DB writes.
- Updated project learnings with the Siemens PLCSIM distinction between quoted DB filter strings and unquoted API-visible tag names.
- Changed `TEST IO` to run audit orchestration asynchronously so the WPF UI can repaint progress instead of freezing until the final report.

### Fixed

- Fixed commissioning DB writes failing with `DoesNotExist` by using the API-visible `dbName.memberName` tag format for reads/writes.
- Fixed the DB tag-list refresh path to pass the DB filter list in Siemens' required quoted format.
- Normalized previously generated commissioning config entries back to the API-visible `dbIBN.testbool1` style.
- Fixed the generated/imported `testreal*` commissioning entries so they are displayed as `Real` values instead of Bool toggles.

### Files

- `Specs/SPEC#002-commissioning-db-import.md`
- `references/s7-plcsim_advanced_function_manual_API_de-DE.pdf`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/CoSimulationPlcSimAdv.csproj`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/Configs/CommissioningDb_dbIBN.json`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/Models/CommissioningDbConfig.cs`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/Models/PLCInstance.cs`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/PlcIo.cs`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/ViewModels/MainWindowViewModel.cs`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/Views/MainWindow.xaml`
- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/Views/MainWindow.xaml.cs`
- `CHANGELOG.md`
- `skills.md`

### Validation

- `& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "Simulator Code\Vacudest\CoSimulationPlcSimAdv\CoSimulationPlcSimAdv.csproj" /t:Build /p:Configuration=Debug /p:Platform=x64`: passed with 0 errors and 2 pre-existing `Simulation.cs` warnings.
- Manual simulator smoke test: imported `dbIBN`, displayed commissioning variables, and confirmed the corrected DB tag naming fixed Bool writes.

### Notes

- TIA watch tables display DB symbols as `"dbIBN".testbool1`, but PLCSIM Advanced API `TagInfos` exposes writable symbols as `dbIBN.testbool1`.
- Siemens' `UpdateTagList` DB filter list still requires a quoted DB name string, for example `"\"dbIBN\""`.
- The PLC project must be updated/downloaded with matching Real DB declarations before Real writes will succeed at runtime; the local `references/dbIBN.db` fixture remains ignored by Git.

