---
name: git-wrap-up
description: "Finish a completed repo task with safe git hygiene: confirm branch, inspect status and diffs, ensure changelog is updated, stage the right files, create a focused commit, and provide a push command without pushing. Use when the user asks to wrap up, finish the session, prepare a commit, commit the completed work, or produce the final push command after a spec or troubleshooting task."
---
# Git Wrap Up

Close a completed task carefully. Treat this as the final gate before a local commit.

## Intake

1. Read `references/AGENTS.md`.
2. Inspect:
   - `git status --short --branch`
   - `git diff --stat`
   - `git diff --check`
   - `git diff -- <relevant paths>` as needed
3. Identify the current branch and whether it matches the task/spec branch.
4. If the branch is `main`, stop and ask before committing.
5. Identify unrelated or surprising changes. Do not stage them silently.

## Changelog Gate

Before staging:

1. Check whether `CHANGELOG.md` has a final dated entry covering the current completed work.
2. The entry should include concrete changes, relevant files, validation status, and notes when useful.
3. If the changelog is missing, stale, or generic, use the `update-changelog` workflow first.
4. Do not commit until the changelog is ready or the user explicitly says to commit without it.


## skills.md Memory Gate

Before staging:

1. Check whether `skills.md` has a summary of any valuable key learnings from this session
2. The entry should be a memory feed for future agent session

## Validation Gate

Confirm the task's required validation was run:

- For a spec task, compare against the spec's `Validation And Testing` and `Acceptance Criteria`.
- For code changes, prefer targeted build/test commands.
- If validation was not run, report that clearly and ask whether to run it or commit with known risk.

## Staging

1. Propose the exact files to stage.
2. Exclude generated artifacts, `.vs/`, `bin/`, `obj/`, and unrelated files.
3. Use path-specific staging, for example:
   - `git add -- path1 path2 path3`
4. After staging, inspect:
   - `git diff --cached --stat`
   - `git diff --cached --check`
5. If staged content is wrong, do not use destructive resets. Ask how to proceed.

## Commit

1. Write one focused commit message.
2. Prefer imperative style:
   - `Add commissioning DB import`
   - `Add git wrap-up skill`
   - `Fix BL170 valve diagnostics`
3. Include docs/spec/changelog updates in the same commit when they belong to the same logical change.
4. Commit locally only after the staged diff matches the intended scope.

## Push Command

Do not push.

After a successful commit, provide a copy-paste command:

- If the branch has no upstream:
  ```powershell
  git push -u origin <branch-name>
  ```
- If the branch already tracks a remote branch:
  ```powershell
  git push origin <branch-name>
  ```

## Final Report

Include:

- current branch
- commit hash and commit subject
- files committed
- validation summary
- changelog status
- push command
- relevant PR comments copy-paste style
- any remaining uncommitted files, if present
