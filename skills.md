# Project Learnings

## PLCSIM Advanced API

- Keep Siemens PLCSIM Advanced API calls on one dedicated worker thread. API events run on separate threads, so marshal UI updates back to WPF.
- Refresh the PLCSIM tag table with `UpdateTagList(...)` before symbolic reads/writes, after hardware/config updates, after reconnecting, and when a read/write reports a stale table.
- Treat `DoesNotExist`, `TypeMismatch`, and `NotUpToData` as different diagnostics. `DoesNotExist` often means the exact symbol is wrong or the tag table is stale.
- Symbol names must match the PLC export exactly. Do not normalize PLC tag names; only normalize UI identifiers.
- In default operating mode, `OnSyncPointReached` is only emitted after each cycle if `IsSendSyncEventInDefaultModeEnabled` is set to `true`.
- `OnOperatingStateChanged` and `OnHardwareConfigChanged` handlers run on separate threads. Stable wrapper events in `PLCInstance` are safer than subscribing the UI directly to an `IInstance` that may be replaced.
- Long diagnostics such as `TEST IO` should orchestrate from a background task and report WPF progress via the dispatcher, while each PLCSIM read/write still goes through the dedicated `PLCInstance` worker thread.

## Current Vacudest Tags

- `references/PLCTags.xlsx` is the authoritative source for current PLC symbols.
- Current tags use hyphenated `IN_*` names such as `IN_LSA-4-1-8-3` and `IN_LC-4-20-8-4`.
- Valve symbols follow explicit groups such as `CTRL_Q21`, `FB_OPN_Q21`, `FB_CLS_Q21`, and matching `_QB` quality bits.
- Simple actuator feedback pairs include `CTRL_G21`/`FB_ON_G21` and `CTRL_E21`/`FB_ON_E21`.

## Simulator Configuration

- `Simulator Code/Vacudest/CoSimulationPlcSimAdv/Configs/DeviceUiTemplate.json` is a hand-fillable template for future config-driven UI device definitions.
- BL170 was integrated surgically from `DeviceUi_BL170.json`: add only BL170-specific devices and leave generic Q21 unchanged unless explicitly requested.
- For many units, prefer a unit sidebar plus config-generated unit panels over a flat type-first list; register generated WPF control names so existing `FindName(...)` device wiring still works.
- Commissioning DB imports are intentionally separate from `DeviceUi_*.json`: generated files use `CommissioningDb_*.json`, PLCSIM API write tags use the `dbName.memberName` spelling surfaced by `IInstance.TagInfos`, and Bool/Real writes should not use marker fallback.
- Before writing imported commissioning DB tags, refresh PLCSIM tags with the DB-specific `UpdateTagList(..., "\"dbName\"")` overload; Siemens documents that the DB filter list itself must start and end with quotes, even though the resulting API-visible tag name is unquoted.
- Real commissioning DB rows only work when the imported `.db` source and the downloaded PLC DB both declare those members as `Real`; changing the simulator config alone cannot overcome a PLC-side Bool declaration.
- Inline commissioning DB structs should be flattened into full member paths such as `dbIBN.BL170.rawWaterSourceTank.tankId`; keep the full PLC path exact, store imported Siemens `Int` declarations as `Int`, and map them to PLCSIM `Int16` reads/writes at runtime.

## Git Hygiene

- Keep `.vs/`, `bin/`, and `obj/` out of future commits; they are IDE/build artifacts and should be ignored rather than reviewed.

## Spec Workflow

- Use `Specs/*.md` for self-contained PRD-style handoff specs for future upgrades; these files should be tracked, not ignored, because they preserve implementation context and git guidance for later agents.
- Pair `create-spec` with `execute-spec`: specs define the contract, and execution should report validation plus acceptance criteria status instead of only summarizing code changes.

## Codex Skills

- Project-local Codex skills can live under `.codex/skills/<skill-name>/` when the workflow should travel with this repo; keep each skill minimal with `SKILL.md` and optional `agents/openai.yaml`.
- Use `warm-up` as the read-only startup ritual for new sessions: read repo guidance, inspect Git status, summarize current state, and stop before edits.
- Use `update-changelog` to keep `CHANGELOG.md` dated and specific, including relevant files and validation notes instead of generic `Unreleased` bullets.
- Use `git-wrap-up` as the end-of-task finisher: verify branch/diff/changelog/validation, stage only intended files, commit locally, and provide a push command without pushing.

## Local Tooling

- On this Windows setup, the `python` alias may point to an inaccessible WindowsApps stub. For skill validation, use the real interpreter at `%LOCALAPPDATA%\Programs\Python\Python313\python.exe`.
