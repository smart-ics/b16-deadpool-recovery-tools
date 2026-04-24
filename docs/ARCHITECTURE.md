\# DEADPOOL BACKUP TOOLS

\## Architecture Overview (Phase-1 MVP)



\## 1. Purpose



Deadpool Backup Tools is a lightweight Windows-based backup automation tool for SQL Server environments (primarily hospitals).



Phase-1 MVP scope:



\- Automated SQL Server backups

\- Copy backup files to Backup Storage Server

\- Backup monitoring

\- Local metadata catalog

\- Structured logging

\- Windows Service runtime



Phase-2 Restore and Phase-3 Auto Recovery are explicitly out of scope.



\---



\# 2. Architecture Style



\## Modular Monolith



Single deployable executable:



```text

deadpool.exe

```



Single solution with modular projects:



```text

Deadpool.sln



src/

&#x20;├── Deadpool.Cli

&#x20;├── Deadpool.Core

&#x20;└── Deadpool.Infrastructure

```



Dependency rule:



```text

Cli -> Core + Infrastructure

Infrastructure -> Core

Core -> nothing

```



Core must not reference Infrastructure.



\---



\# 3. Technology Decisions



| Concern | Decision |

|--------|----------|

Runtime | .NET Worker Service + Console Mode |

Scheduler | Quartz.NET |

Metadata DB | SQLite |

Data Access | Dapper |

Logging | Serilog |

Backup Engine | Native T-SQL BACKUP commands |

Deployment | Portable folder + Windows Service registration |



\---



\# 4. Deployment Model



Portable deployment:



```text

C:\\Deadpool\\

&#x20; deadpool.exe

&#x20; deadpool.db

&#x20; appsettings.json

&#x20; logs\\

```



Service registration handled manually:



```powershell

sc create Deadpool ...

sc start Deadpool

```



Supports dual mode:



```text

deadpool --console

deadpool --service

```



\---



\# 5. Core Modules



\## Deadpool.Cli

Responsibilities:



\- Application entry point

\- Host configuration

\- Quartz scheduler registration

\- Dependency injection

\- Windows Service hosting



\---



\## Deadpool.Core

Responsibilities:



\- Domain entities

\- Interfaces

\- Backup workflow orchestration

\- State machine

\- Scheduling abstractions

\- Business rules



Contains no infrastructure concerns.



\---



\## Deadpool.Infrastructure

Responsibilities:



\- SQL Server backup execution

\- SQLite persistence

\- File copy logic

\- Logging bootstrap

\- Precheck implementations



\---



\# 6. Domain Models



\## Entities



\## Server

```text

ServerId

ServerName

Role

IPAddress

SqlInstance

IsActive

LastHeartbeat

```



\## DatabaseProfile

```text

DatabaseId

ServerId

DatabaseName

RecoveryModel

LogBackupEnabled

FullBackupSchedule

DiffBackupSchedule

LogBackupEveryMinute

RetentionDays

```



\## BackupCatalog

```text

BackupId

DatabaseId

BackupType

BackupDate

BackupFile

StoragePath

FileSizeMB

Status

Verified

ParentBackupId

CopyAttempts

ErrorMessage

```



\## BackupJob

```text

JobId

DatabaseId

BackupType

ScheduledAt

StartedAt

CompletedAt

State

ErrorMessage

```



\---



\# 7. Backup Types



Supported:



```text

Full

Differential

TransactionLog

```



Backup extensions:



```text

.bak  -> Full / Differential

.trn  -> Transaction Log

```



Naming convention:



```text

DB\_FULL\_yyyyMMdd\_HHmmss.bak

DB\_DIFF\_yyyyMMdd\_HHmmss.bak

DB\_LOG\_yyyyMMdd\_HHmmss.trn

```



\---



\# 8. Backup Scheduling



Cron-based scheduling.



Examples:



```text

Full Backup:

0 2 \* \* SUN



Differential:

0 3 \* \* MON-FRI



Log Backup:

\*/15 \* \* \* \*

```



Rules:



\- One active backup job per database

\- No overlapping jobs for same database

\- Parallel execution across different databases allowed



Quartz misfire policy:



\- Full/Diff -> FireAndProceed

\- Log -> DoNothing



\---



\# 9. Backup Workflow State Machine



States:



```text

Pending

Running

BackupCompleted

Copying

Verified

Success

Failed

RetryPending

```



Workflow:



```text

Trigger

\-> Precheck

\-> Execute Backup

\-> Validate Backup File

\-> Copy To Storage

\-> Verify Copy

\-> Update Catalog

\-> Success

```



Copy failure:



```text

Retry up to 3 times

```



Backup failure:



```text

No automatic retry

```



\---



\# 10. Prechecks



Before backup runs:



Validate:



\- Database online

\- Sufficient disk space

\- Backup path writable

\- Full backup exists (for differential)

\- FULL recovery model (for log backup)



\---



\# 11. Backup Execution Strategy



Use native T-SQL backup commands.



\## Full Backup

Uses:



\- COMPRESSION

\- CHECKSUM

\- INIT

\- STATS



No COPY\_ONLY.



\---



\## Differential Backup

Uses:



\- DIFFERENTIAL

\- COMPRESSION

\- CHECKSUM



\---



\## Log Backup

Uses:



\- BACKUP LOG

\- CHECKSUM



Only valid for FULL recovery model.



\---



\## Validation

After backup:



```text

File exists

File size > 0

RESTORE VERIFYONLY

```



\---



\# 12. File Copy Strategy



Strategy:



```text

1 Create backup locally

2 Validate local backup

3 Copy to storage server

4 Verify copied file

5 Update catalog

```



Never back up directly to network share.



Storage layout:



```text

\\Backup

&#x20; \\HospitalA

&#x20;   \\FULL

&#x20;   \\DIFF

&#x20;   \\LOG

```



Copy retries:



```text

3 attempts

```



\---



\# 13. Retention



Retention cleanup included in MVP.



Policy:



```text

Local backups: 3 days

Storage backups: 14 days

```



Cleanup runs as scheduled maintenance job.



\---



\# 14. Logging



Structured logging required.



Log examples:



```text

Backup started

Backup completed

Copy retry

Verification failed

Cleanup deleted files

```



Logs stored:



```text

logs/deadpool-yyyyMMdd.log

```



\---



\# 15. Out Of Scope (MVP Exclusions)



Not part of Phase-1:



\- Restore automation

\- Auto recovery

\- Log shipping

\- Remote centralized management

\- Encryption

\- GUI

\- Distributed agents

\- Microservices

\- External compression tools



\---



\# 16. Implementation Order



Recommended build order:



1 Domain models

2 BackupWorkflow state machine

3 SQLite catalog

4 SQL backup executor

5 File copy service

6 Quartz scheduler

7 Windows Service host

8 Retention cleanup



\---



\# 17. Engineering Principles



Rules:



\- Keep MVP simple

\- Prefer pragmatism over abstraction

\- Avoid overengineering

\- Clean code

\- SOLID where useful

\- Modular monolith remains single executable

\- Incremental implementation by module



\---



\# 18. Source of Truth



This document is the architecture source-of-truth for implementation.



If code and this document diverge:



Architecture decisions in this document win unless intentionally revised.

