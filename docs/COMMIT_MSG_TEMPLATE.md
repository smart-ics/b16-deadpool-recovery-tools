Commit message template for configuration changes
===============================================

Use this template when committing configuration-related housekeeping (adding examples, updating .gitignore, removing tracked local files).

Subject (imperative, short):
--------------------------------
chore(config): add appsettings.example and stop tracking local runtime files

Body (optional, explain why and what):
------------------------------------------------
Why: prevent local secrets and build artifacts from being accidentally committed. Provide a committed example so developers can create local config quickly.

What changed:
- Added: `src/Deadpool.Cli/appsettings.json.example`
- Added: `.gitignore` rules to ignore build outputs, logs, and local appsettings
- Stopped tracking (removed from index): local `src/Deadpool.Cli/appsettings.json`, `**/bin/`, `**/obj/`, `src/**/logs/`

How to verify:
- Ensure `src/Deadpool.Cli/appsettings.json` is present locally but not tracked: `git status` should not show it.
- Build and run locally: `dotnet run --project src/Deadpool.Cli`

Commands used:
```powershell
git add .gitignore src/Deadpool.Cli/appsettings.json.example
git rm -r --cached src/Deadpool.Cli/bin src/Deadpool.Cli/obj src/Deadpool.Core/bin src/Deadpool.Core/obj src/Deadpool.Infrastructure/bin src/Deadpool.Infrastructure/obj
git rm --cached src/Deadpool.Cli/appsettings.json || $true
git commit -m "chore(config): add appsettings.example and stop tracking local runtime files"
```

Issue/reference: (optional) #<issue-number>

Signed-off-by: <Your Name> <you@example.com>

