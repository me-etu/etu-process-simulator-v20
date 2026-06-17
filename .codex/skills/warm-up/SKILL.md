---
name: warm-up
description: Start a repo session with a read-only senior-engineer warm-up for this TIA Portal simulator repo. Use when the user says "please warm-up", "warm up", "start session", "session warmup", or asks Codex to read project guidance before receiving the task.
---

# Warm Up

Prepare context before any implementation. This skill is read-only unless the user gives a separate follow-up task.

## Steps

1. Act as a careful senior engineer on this TIA Portal simulator repo.
2. Read project guidance and handoff notes:
   - `references/AGENTS.md`
   - `skills.md`
   - `references/SIMULATOR_HANDOFF.md`
   - `Simulator Code/Vacudest/PlcSimAdvancedFramework/README.md`
3. If a listed file is missing, use `rg --files` to find the closest matching `AGENTS.md`, `README.md`, `SIMULATOR_HANDOFF.md`, `SKILLS.md`, or `skills.md`, then report the resolved path.
4. Run `git status --short --branch`.
5. Do not edit files, run builds, install dependencies, pull, push, or use network access during warm-up.

## Summary Format

After reading, summarize:

- current branch and working tree state
- important repo rules from `AGENTS.md`
- current simulator architecture and handoff quirks
- key PLC/tag/config lessons from `skills.md`
- recommended targeted build/test command
- any immediate caution before the next task

End by saying that no edits were made and that you are ready for the task.
