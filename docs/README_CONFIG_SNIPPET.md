Configuration quickstart
========================

Purpose
-------
This snippet shows the minimal steps developers need to bootstrap local configuration from the committed example and run the app locally.

Steps (PowerShell)
-------------------

1. From the repository root, copy the example into a local file that will be ignored by Git:

```powershell
cd 'D:\Project.Aktif\_Published\b16-deadpool-recovery-tools'
Copy-Item -Path 'src/Deadpool.Cli/appsettings.json.example' -Destination 'src/Deadpool.Cli/appsettings.json' -Force
```

2. Edit `src/Deadpool.Cli/appsettings.json` and replace placeholders (e.g. connection string, backup paths).

3. Verify the file is ignored (should not appear in `git status`):

```powershell
git status --short
```

4. Run the service locally to validate the configuration:

```powershell
dotnet run --project src/Deadpool.Cli
```

Notes
-----
- The repository contains `src/Deadpool.Cli/appsettings.json.example` (committed). The real `appsettings.json` is ignored by `.gitignore` to avoid committing secrets.
- Prefer environment variables or dotnet user-secrets for sensitive values in development.

