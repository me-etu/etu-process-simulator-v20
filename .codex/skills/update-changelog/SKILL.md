---
name: update-changelog
description: Update CHANGELOG.md with dated, specific, audit-friendly project entries. Use when the user asks to update, improve, format, normalize, or add to the changelog, release notes, change tracker, or project history with files changed, validation, notes, and Added/Changed/Fixed sections.
---

# Update Changelog

Maintain `CHANGELOG.md` as an engineering audit trail, not a generic bullet bucket.

## Intake

1. Read `references/AGENTS.md`.
2. Read `skills.md`.
3. Read the current `CHANGELOG.md`.
4. Inspect `git status --short --branch`.
5. Inspect relevant diffs with `git diff -- <paths>` when the changelog entry is based on current work.

## Format

Use date-based entries:

```markdown
## YYYY-MM-DD

### Added

- ...

### Changed

- ...

### Fixed

- ...

### Files

- `path/to/file`

### Validation

- `command`: result.

### Notes

- ...
```

Use only sections that have real content, except `Files` and `Validation` should be included for implementation entries when the information is known.

## Writing Rules

- Prefer concrete behavior and file-level specificity over vague summaries.
- Mention important source-of-truth files, generated files, config files, and project files.
- Record build/test commands and results when they were run.
- If validation was not run, state that plainly and give the reason.
- Do not claim a fix, test, or build succeeded unless there is evidence.
- Preserve historical entries; when converting an older generic section, note if exact original dates are not known.
- Keep bullets concise but useful to a future maintainer.

## Categories

- Use `Added` for new files, features, workflows, configs, skills, docs, or diagnostics.
- Use `Changed` for behavior, architecture, build setup, formatting, or process changes.
- Use `Fixed` for confirmed bugs or regressions.
- Use `Files` for touched or relevant files.
- Use `Validation` for commands run and their outcomes.
- Use `Notes` for source-of-truth decisions, limitations, warnings, or follow-up context.

## Final Check

Before finishing:

- Confirm the changelog has a date header for the entry.
- Confirm meaningful implementation entries mention relevant files.
- Confirm validation is recorded or explicitly marked not run.
- Report changed files.
