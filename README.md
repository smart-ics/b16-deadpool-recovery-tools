# b16-deadpool-recovery-tools

Deadpool Backup Tools — repository for backup workflow, executors and CLI for scheduling and copying backups.

Configuration
-------------
Local configuration is not committed to the repository to avoid accidental secret leakage. See the configuration quickstart for how to create a local `appsettings.json` from the committed example:

- docs/README_CONFIG_SNIPPET.md
- docs/CONFIGURATION.md (more details and alternatives for secrets)

Quick copy example (PowerShell):

```powershell
cd 'D:\Project.Aktif\_Published\b16-deadpool-recovery-tools'
Copy-Item -Path 'src/Deadpool.Cli/appsettings.json.example' -Destination 'src/Deadpool.Cli/appsettings.json' -Force
```

After copying, edit `src/Deadpool.Cli/appsettings.json` to fill in database connection strings and storage paths. The real `appsettings.json` is ignored by `.gitignore`.

Contributing
------------
Please follow the existing code style and add tests for new behavior. Use the provided appsettings example or environment variables/dotnet user-secrets for local development.

