# Codex safety rules for this project

- Work only inside this project folder.
- Do not access Documents, Desktop, Downloads, OneDrive, browser profiles, SSH keys, credentials, or other folders.
- Do not delete files unless I explicitly ask.
- For Word, Excel, PowerPoint, and PDF files, create a new edited copy instead of overwriting the original.
- Before making large changes, explain the plan first.
- Keep changes small and reviewable.
- Do not use network access unless I approve it.
- Summarize every file changed.
- Create and update skills.md file with the gained experiences from this project.


## Git Workflow

- Before starting a change, inspect `git status` and understand any existing user or staged changes.
- Monitor `.gitignore`, advise if any files need to move there or any cleanup necessary.
- Sync with `main` before new work when network/remote access is available and the user has approved it.
- Create a small, focused branch from `main` for each task.
- Keep commits surgical: one logical change per commit, with docs/tests included when relevant.
- Do not mix unrelated cleanup with feature or bug-fix work.
- Push the branch only when the user asks or has approved remote access.
- Never use destructive Git commands such as `reset --hard`, `clean`, or checkout-based reverts unless the user explicitly requests them.

## Change Tracking

- Update `CHANGELOG.md` for meaningful behavior, dependency, or generator changes.
- Update `skills.md` with session learnings, quirks, bug causes, and fixes that future agents should remember.
- Keep `README.md` current when setup, build, or runtime assumptions change.
