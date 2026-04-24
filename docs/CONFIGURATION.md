Configuration and local development
=================================

This document explains how to create a local `appsettings.json` from the committed example and best practices for keeping secrets/configuration out of source control.

Quick steps (PowerShell)
------------------------

1. Copy the example to create your local config (run from the repository root):

```powershell
cd 'D:\Project.Aktif\_Published\b16-deadpool-recovery-tools'
Copy-Item -Path 'src/Deadpool.Cli/appsettings.json.example' -Destination 'src/Deadpool.Cli/appsettings.json' -Force
```

2. Edit `src/Deadpool.Cli/appsettings.json` and replace placeholders:
- `SqlConnectionString` — fill database server, database name and credentials (or use integrated auth)
- `LocalBackupRoot` — a local folder where backups will be staged
- `StorageRoot` — network share or cloud storage path

3. Do NOT commit `src/Deadpool.Cli/appsettings.json`.
   - The repository already contains a `.gitignore` that excludes `src/**/appsettings.json` and common build outputs.
   - If `appsettings.json` is currently tracked, untrack it with:

```powershell
git rm --cached src/Deadpool.Cli/appsettings.json
git commit -m "chore: stop tracking local appsettings.json"
```

Alternatives for secrets
------------------------
- Environment variables: read secrets from env vars instead of storing them in files.
- dotnet user-secrets (for local development):

```powershell
dotnet user-secrets init --project src/Deadpool.Cli
dotnet user-secrets set "Deadpool:SqlConnectionString" "Server=...;Database=...;User Id=...;Password=...;" --project src/Deadpool.Cli
```

- Use a secrets manager (Azure Key Vault, AWS Secrets Manager) for production.

Verify and run
--------------
- After creating `appsettings.json`, run the application locally to verify configuration:

```powershell
dotnet run --project src/Deadpool.Cli
```

Notes
-----
- `deadpool.db` (SQLite) and other runtime files are ignored by `.gitignore`. Do not commit local database files.
- Keep `appsettings.json.example` up to date with any new configuration keys so new developers can bootstrap quickly.

