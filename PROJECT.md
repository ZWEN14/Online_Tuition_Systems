# Online Tuition System — Project Record

## 1. Purpose of This Document

This file is the project source of truth for confirmed scope, decisions, environment state, completed milestones, current work, ownership, and handoff context.

Read `AGENTS.md` before using this file. `AGENTS.md` controls how Codex and other coding assistants may work in the repository.

Do not mark an implementation task complete merely because code was proposed. Mark it complete only after the user confirms that the change was manually applied and works, or after the repository already contains verified working implementation.

## 2. Project Summary

- Course: BMIT2023 Web and Mobile Systems
- Application: Online Tuition System
- Framework: ASP.NET Core MVC
- Language: C#
- Target framework: .NET 10 (`net10.0`)
- ORM: Entity Framework Core, code-first
- Database target: SQL Server Express
- Final database requirement: file-based SQL Server database
- Database file terminology: use `.mdf` for the SQL Server data file and `.ldf` for its log; `.mdb` refers to Microsoft Access and is not the current project database choice
- Authentication requirement: manual cookie-based authentication; do not use ASP.NET Core Identity
- Primary editor: Visual Studio Code on Windows
- Secondary tool: Visual Studio Community 2026 when SQL Server tooling or compatibility checking is useful
- Coding assistant: Codex with project-only editing permission

## 3. Confirmed Working Rules

- Codex may create and edit files only inside this repository for the user's requested task.
- Codex may inspect allowed repository context and run safe read-only diagnostics.
- Codex must not modify files outside the repository, installed packages, database state, Git state, PATH, services, extensions, SDKs, or system settings.
- Codex must not run state-changing terminal commands; the user runs those commands manually.
- Codex briefly summarizes project-file changes after editing.
- Every proposed change must identify the task part, filename, class/method/section, current line number or precise nearby anchor, whether to add/replace/remove, and the exact content.
- Never expose secrets or inspect secret-bearing files unnecessarily.
- Preserve existing user work and do not discard generated or tracked files without the user's informed decision.

## 4. Confirmed Environment Decision

The current Windows laptop uses a native development workflow:

- Run the ASP.NET Core application directly with the Windows .NET 10 SDK.
- Use VS Code for normal development and Codex repository context.
- Use SQL Server Express LocalDB for local development and the required file-based SQL Server workflow.
- Do not add Docker Compose or a devcontainer on this machine unless a later requirement makes it necessary and the user changes this decision.
- Docker is not required merely to use SQL Server: LocalDB supplies the lightweight SQL Server Express-compatible development option on Windows.

## 5. Environment Audit — 2026-09-07

Observed as installed or available:

- .NET SDK `10.0.400`
- ASP.NET Core runtime `10.0.11`
- Project targets `net10.0`
- Visual Studio Code `1.136.1`
- C# extension files installed
- C# Dev Kit extension files installed
- Codex extension installed
- Visual Studio Community 2026 `18.9.1`
- Git installed at `C:\Program Files\Git\cmd\git.exe`
- SQL Server LocalDB command-line tooling installed
- `sqlcmd` installed
- `MSSQLLocalDB` instance running with SQL Server 2025 Express `17.0.4025.3`
- LocalDB connectivity verified successfully with Windows Authentication and `sqlcmd`
- Docker CLI and Docker Compose installed, but the Docker Desktop Linux engine was not running during the audit
- WSL 2 configured with Ubuntu as the default distribution

Observed setup gaps:

- no EF Core SQL Server package references are present
- no application `DbContext` is present
- no database connection string is present
- no migrations are present
- generated `bin/` and `obj/` content has already been committed or tracked
- no `compose.yml` or `.devcontainer/` configuration is present; this is intentional for the current native Windows decision

## 6. Current Repository State

The repository currently contains the standard initial ASP.NET Core MVC template, including:

- `Online_Tuition_Systems.csproj`
- `Program.cs`
- `Controllers/HomeController.cs`
- the starter `Models`, `Views`, and `wwwroot` content

`Online_Tuition_Systems.csproj` currently has no explicit NuGet package references. `Program.cs` currently registers MVC and the normal starter request pipeline, but no EF Core context, authentication, or application services.

Git audit notes:

- branch: `main`
- remote tracking: `origin/main`
- latest observed commit: `18445de` (`two`)
- `AGENTS.md` was untracked at audit time
- `debug.log` was untracked at audit time
- multiple tracked files under `obj/` had local modifications

Do not clean, discard, untrack, or commit any of these items automatically. The user must review and perform Git changes manually.

## 7. Completed Work

- Initial ASP.NET Core MVC template exists.
- Project target is .NET 10.
- Windows development environment was audited.
- Native Windows + VS Code + LocalDB was selected as the current environment direction.
- Git is available from the VS Code PowerShell terminal.
- The `MSSQLLocalDB` SQL Server Express instance is running and its connection was verified.
- A project `.gitignore` was added for .NET build output, local logs, IDE state, test output, and local secret overrides.
- Repository assistant rules were clarified in `AGENTS.md`.
- This project handoff record was created.

No EF Core data layer, LocalDB database, migration, authentication module, or business module has been confirmed complete.

## 8. Immediate Next Steps

1. Verify that C#, C# Dev Kit, Codex, and SQL Server (mssql) are enabled in the active VS Code profile.
2. Remove already-tracked `bin/` and `obj/` content from Git tracking manually without deleting the local files.
3. Add the EF Core 10 SQL Server provider and design-time tooling manually after reviewing the exact package proposal.
4. Confirm the domain entities and relationships before creating `DbContext` or migrations.
5. Configure the file-based `.mdf` development path after confirming the exact assignment wording and final-delivery expectation.

## 9. Planned Major Sections

These sections are planned but not yet confirmed complete:

1. Native Windows development environment and Git hygiene
2. Domain requirements and entity relationship design
3. EF Core context, SQL Server connection, and initial migration
4. Manual cookie authentication and role authorization
5. Core Online Tuition System modules
6. Validation, security review, and end-to-end verification
7. Final SQL Server Express file/database preparation
8. Submission review and documentation

Module ownership and detailed business scope have not yet been recorded. Ask the user for the assignment specification and team ownership before assigning or expanding modules.

## 10. Decision and Activity History

### 2026-09-07 — Initial environment audit

- Confirmed that the repository targets .NET 10 and uses the ASP.NET Core MVC starter template.
- Confirmed that the required .NET 10 SDK and ASP.NET Core runtime are installed.
- Confirmed that VS Code is the preferred primary editor and Visual Studio 2026 is an optional secondary tool.
- Confirmed that Docker/devcontainer overhead is not wanted for the current Windows workflow.
- Selected SQL Server Express LocalDB as the preferred local database direction.
- Clarified that the expected SQL Server database file is `.mdf`, not Access `.mdb`, subject to checking the exact assignment wording.
- Identified Git PATH, LocalDB instance creation, Git ignore/tracking, EF Core, and data-layer setup as the next environment tasks.
- Established the initial read-only code-assistance rule; this was superseded on 2026-09-08 by project-only editing permission.

### 2026-09-08 — Project-only editing permission

- The user authorized Codex to create and edit files inside this repository for requested tasks.
- Codex remains prohibited from modifying the operating system, installed tools, packages, database state, or Git state.
- State-changing terminal commands remain manual user actions.
- Responses should briefly summarize changes unless the user asks for more detail.

### 2026-09-08 — Git and LocalDB verification

- Confirmed Git `2.55.0.windows.3` is available from the VS Code PowerShell terminal.
- Confirmed the automatic `MSSQLLocalDB` instance is running SQL Server 2025 Express `17.0.4025.3`.
- Confirmed Windows Authentication connectivity using `sqlcmd`.
- Confirmed the MSSQL extension should leave the Database field empty until the application database exists; do not type the literal text `<Default>`.
- Added `.gitignore`; previously tracked build artifacts still require a manual Git index cleanup.
