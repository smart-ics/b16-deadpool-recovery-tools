\# DEADPOOL BACKUP TOOLS

\## Architecture Decisions (ADR Log)



This document records major architecture and technical decisions for Phase-1 MVP.



Status:



```text

Accepted = Final Decision

Deferred = Consider later

Rejected = Intentionally not chosen

```



\---



\# ADR-001

\## Use Modular Monolith Architecture



Status:

Accepted



Decision:



Use modular monolith architecture with a single deployable executable.



Structure:



```text

Deadpool.Cli

Deadpool.Core

Deadpool.Infrastructure

```



Rationale:



\- Faster MVP delivery

\- Lower complexity

\- Easier deployment in hospital environments

\- Easier AI-assisted development

\- Avoid distributed system complexity



Rejected alternatives:



\- Microservices

\- Agent + daemon architecture (deferred for Phase-3)



\---



\# ADR-002

\## Use Portable Deployment + Windows Service



Status:

Accepted



Decision:



Deploy as portable folder and register manually as Windows Service.



```text

Copy folder

Register service

Run

```



Rationale:



\- No installer complexity

\- Server-friendly deployment

\- Auto-start on reboot

\- Low operational overhead



Rejected:



\- MSI installer

\- GUI-first desktop application



\---



\# ADR-003

\## Use SQLite as Local Metadata Catalog



Status:

Accepted



Decision:



Use SQLite embedded database for Deadpool metadata.



Rationale:



\- Lightweight

\- Zero administration

\- Perfect for metadata workload

\- Simple deployment

\- Single-file storage



Additional:



Use WAL mode.



```text

PRAGMA journal\_mode=WAL

```



Rejected:



\- SQL Server Express

\- PostgreSQL (deferred for centralized management)

\- LiteDB

\- Access



\---



\# ADR-004

\## Use Dapper for Data Access



Status:

Accepted



Decision:



Use Dapper for SQLite persistence.



Rationale:



\- Lightweight

\- Simple schema

\- Fast for MVP

\- Team familiarity

\- Avoid EF Core complexity



Rejected:



\- EF Core



\---



\# ADR-005

\## Use Quartz.NET Internal Scheduler



Status:

Accepted



Decision:



Use internal cron-based scheduling via Quartz.NET.



Rationale:



\- Mature scheduler

\- Supports cron expressions

\- Misfire handling

\- Supports per-database scheduling



Rejected:



\- Polling loop scheduler

\- External Windows Task Scheduler

\- Custom scheduler implementation



Misfire policy:



```text

Full/Diff -> FireAndProceed

Log -> DoNothing

```



\---



\# ADR-006

\## Use Serialized Execution Per Database



Status:

Accepted



Decision:



Allow only one active backup job per database.



Rules:



```text

No overlapping jobs per database

Parallel across databases allowed

```



Rationale:



\- Preserve backup chain integrity

\- Avoid overlapping backup operations

\- Prevent scheduler collisions



Clarification:



This is a job synchronization lock,

not SQL Server database locking.



\---



\# ADR-007

\## Use Generic Backup Workflow State Machine



Status:

Accepted



Decision:



Use explicit state machine for all backup types.



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



Rationale:



\- Observable workflow

\- Retry support

\- Consistent monitoring

\- Foundation for later recovery automation



\---



\# ADR-008

\## Use Generic Backup Executor



Status:

Accepted



Decision:



Single generic backup executor supports:



```text

Full

Differential

TransactionLog

```



Not separate executors per type.



Rationale:



\- Less duplication

\- Simpler workflow integration

\- Easier testing



\---



\# ADR-009

\## Use Native SQL Server Backup Features



Status:

Accepted



Decision:



Use native T-SQL backup options:



```text

COMPRESSION

CHECKSUM

INIT

RESTORE VERIFYONLY

```



Do not use:



```text

COPY\_ONLY

```



Rationale:



\- Leverage SQL Server capabilities

\- Higher integrity

\- Less external complexity



Rejected:



\- External compression tools

\- Backup directly to network share



Strategy:



```text

Backup local first

Copy after validation

```



\---



\# ADR-010

\## Use Local-First Backup Then Copy



Status:

Accepted



Decision:



Backups are written locally first, then copied to storage server.



Workflow:



```text

Backup

Validate

Copy

Verify copy

```



Rationale:



\- Avoid SQL Server UNC permission issues

\- More reliable

\- Easier failure handling



Rejected:



\- Direct backup to network share



\---



\# ADR-011

\## Copy Retry Policy



Status:

Accepted



Decision:



Copy failures retry up to:



```text

3 attempts

```



Backup execution failures:



```text

No automatic retry

```



Rationale:



\- Copy failures often transient

\- Backup failures should be investigated



\---



\# ADR-012

\## Include Retention Cleanup in MVP



Status:

Accepted



Decision:



Retention cleanup included in Phase-1.



Policy:



```text

Local: 3 days

Storage: 14 days

```



Rationale:



\- Prevent disk exhaustion

\- Operational necessity

\- Should not wait until later phases



\---



\# ADR-013

\## Logging Strategy



Status:

Accepted



Decision:



Use structured logging via Serilog.



Log categories:



\- Scheduler

\- Backup execution

\- Copy operations

\- Validation

\- Cleanup

\- Errors



Rationale:



\- Troubleshooting

\- Auditability

\- Production diagnostics



\---



\# ADR-014

\## Phase-1 Scope Boundaries



Status:

Accepted



Explicitly excluded from MVP:



```text

Restore automation

Auto recovery

Log shipping

Remote centralized management

GUI

Encryption

Distributed agents

Microservices

```



Rationale:



Protect MVP scope.



\---



\# ADR-015

\## Solution Topology



Status:

Accepted



Use single solution:



```text

Deadpool.sln

src/

tests/

docs/

```



Documentation in repo:



```text

ARCHITECTURE.md

DECISIONS.md

```



These documents are implementation source-of-truth.



\---



\# Deferred Decisions



Not rejected, postponed.



\---



\## DDR-001

Agent + Daemon Architecture



Status:

Deferred (Phase-3)



Potential future:

Distributed agents with central controller.



\---



\## DDR-002

Central Metadata Database



Status:

Deferred



Potential future:



```text

PostgreSQL centralized control plane

```



\---



\## DDR-003

Checksum Hash Verification on File Copy



Status:

Deferred



Current MVP:

File size verification only.



Possible future:

SHA256 verification.



\---



\## DDR-004

Dry Run / Simulation Mode



Status:

Deferred but desirable



Potential command:



```text

deadpool run --dry-run

```



\---



\# Decision Rule



If implementation conflicts with this document:



```text

DECISIONS.md wins

unless intentionally superseded by new ADR.

```



New decisions must be added as new ADR entries.

