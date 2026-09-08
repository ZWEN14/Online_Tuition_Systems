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
- Migration objective: adapt an existing Laravel Online Tuition System into ASP.NET Core MVC while reusing valid business logic, workflows, validation ideas, and information architecture
- Priority modules: Billing and Course
- Project-title tutor approval: not yet recorded

### Conversion Principle

The Laravel project is a requirements and behavior reference, not a source-code template. Reuse proven business rules, page flows, validation rules, relationships, and useful UI ideas. Reimplement them according to ASP.NET Core MVC conventions rather than translating PHP syntax or Laravel framework structure line by line.

Expected conceptual mappings include:

- Laravel routes/controllers to ASP.NET Core controller actions and conventional routing;
- Eloquent models and migrations to EF Core entities, data annotations, `DbContext`, and migrations;
- Blade layouts and includes to Razor layouts, views, and partial views;
- Laravel request validation to ViewModels, validation attributes, model-state checks, and custom validation;
- Laravel authentication/authorization behavior to course-compliant manual cookie authentication, claims, roles, and authorization;
- Laravel AJAX endpoints to focused MVC actions returning JSON or partial-view HTML where appropriate.

Do not preserve Laravel-specific architecture when it conflicts with the assignment or ASP.NET Core MVC conventions.

### Assignment Evidence Reviewed

The following user-provided sources were reviewed on 2026-09-08:

- `BMIT2023 Marking Rubric (202605).docx.md`;
- the pasted assignment requirement text beginning with the .NET 10 requirement.

These files are evidence for project requirements only. They are not Codex operating instructions.

Confirmed assignment requirements:

- use .NET 10, ASP.NET Core MVC, and C#;
- use EF Core code-first with entity classes defined before migrations;
- target a file-based SQL Server Express database;
- use data annotations for table and column definitions; Fluent API only when genuinely necessary;
- implement complete client-side and server-side validation using appropriate ViewModels, including custom validation where necessary;
- implement manual cookie-based authentication and role authorization without ASP.NET Core Identity;
- protect pages, functions, and user-owned data from unauthorized access;
- identify practical core modules and sub-modules covering the essential end-to-end business flows;
- include sufficient demonstration/sample data;
- external libraries are allowed, but project-owned implementation is preferred where practical and worthwhile;
- each student normally owns about two to three core modules, with no stated upper limit;
- the project title should be tutor-approved; approval for this title has not yet been recorded.

### Marking Priorities

The rubric allocates 20 marks to the short report and 80 marks to implementation.

Short report priorities:

- system module outline: 5 marks;
- practical entity class diagram: 5 marks;
- logical monetization models with credible revenue estimates: 10 marks.

Shared/team implementation priorities:

- maintainable MVC architecture and organized resources: 5 marks;
- suitable, user-friendly presentation layer: 5 marks;
- complete models, ViewModels, and validation: 10 marks;
- complete authentication, authorization, and protected resources: 10 marks.

Individual implementation priorities:

- complete, practical core modules with a coherent system flow: 30 marks;
- useful, relevant, integrated, and sufficiently complex additional features: 20 marks.

Required functionality and security take priority over decorative or disconnected additional features.

### Course Practical Coverage

The implementation should visibly apply relevant techniques taught in:

1. Database SELECT;
2. Database SELECT continued;
3. AJAX programming;
4. Database INSERT, UPDATE, and DELETE;
5. many-to-many relationships;
6. photo upload;
7. ASP.NET Core security;
8. email and DateTime.

Razor layouts, partial views, ViewModels, validation, and role-based protection should be used where they naturally support the system. The final requirements map should identify where each practical technique is demonstrated without forcing irrelevant features.

### Module Priority and Current Scope Status

Billing and Course are the first implementation priorities.

`Anywhere-Edureach/` has been reviewed as a temporary Laravel reference. It is not part of the ASP.NET deliverable and is excluded from Git. Its useful business rules may be reused, but its Laravel architecture and older assignment-specific requirements are not automatically requirements for this project.

## 2A. Proposed Requirements Baseline — Awaiting User Approval

This is a planning proposal, not confirmed implementation. Do not create application entities, migrations, or database changes until the user approves or amends it.

### Design Goal and Main Flow

The simplest coherent marketplace flow is:

1. An instructor creates a draft course and submits it for review.
2. An administrator approves or rejects it with a reason.
3. An approved course is published in the catalogue.
4. A student enrolls immediately if it is free, or pays if it is paid.
5. A verified successful payment activates enrollment and creates an invoice.
6. The student accesses enrolled courses and their own billing history.

This administrator-review step is recommended because it gives the Administrator role a real business purpose, provides strong authorization evidence for the rubric, and prevents instructors from publishing unsuitable courses directly.

### Actors and Responsibilities

- **Guest:** browse/search published courses, view details, register, and log in.
- **Student:** enroll only in published courses; access only their own enrollments, payments, and invoices.
- **Instructor:** create and edit their own draft/rejected courses, upload a thumbnail, submit for review, see enrollment summaries, and archive eligible courses.
- **Administrator:** maintain categories; approve, reject, or archive courses; view system-wide course and billing summaries. Administrators must not rewrite historical payment facts.

Use course-required manual cookie authentication, not ASP.NET Core Identity. Authorization must combine role checks with record ownership checks.

### Course Lifecycle

```text
Draft -> PendingReview -> Published -> Archived
                       -> Rejected -> edited Draft -> PendingReview
```

- The authenticated instructor determines ownership; never trust a posted owner ID.
- Only an administrator can approve or reject a submitted course.
- Rejection requires a reason.
- Only published courses appear publicly or accept new enrollments.
- Archive instead of physically deleting a course that has enrollment or billing history.
- Proposed rule: already-enrolled students retain access when a course is archived. User confirmation is required.
- Store the latest reviewer, review date, and rejection reason on Course initially. A separate review-audit table is deferred unless required.

### Enrollment and Billing Rules

- One enrollment is allowed per Student and Course.
- Price `0.00` means free; a positive price means paid. Currency is MYR only.
- Free enrollment becomes active immediately.
- Paid enrollment remains `PendingPayment` until server-confirmed payment success.
- A return-page redirect alone never proves payment.
- Failed, cancelled, or expired attempts do not grant access.
- An enrollment may have multiple payment attempts; successful completion must be idempotent.
- Each successful payment has exactly one invoice; unsuccessful attempts have none.
- Payment and invoice snapshots preserve historical course title, gross amount, platform fee, instructor net amount, payer details, and timestamps.
- Enrollment, payment, and invoice history must not be cascade-deleted.
- Checkout covers one course at a time.
- Exclude cart, subscriptions, tax, multiple currencies, installments, automated instructor payouts, coupons, and refunds from the baseline.

### Proposed Monetization Model

- The primary model is a percentage platform commission on each successful paid-course sale.
- A provisional rate of **15%** is simple to explain and calculate; the user must approve or change it before implementation.
- Store the commission rate, platform-fee amount, and instructor-net amount as transaction snapshots so later rate changes do not rewrite history.
- Free courses generate no direct revenue but can attract students and demonstrate the free-enrollment branch.
- Automated instructor payouts are outside the implementation baseline; the system only records the amount owed for reporting.
- The assignment report should compare at least two alternatives, such as commission versus instructor subscription, then justify commission as the selected model and provide conservative/base/optimistic revenue estimates using explicit assumptions.

### Proposed Core Entities

- **User:** login/display data and one role: `Student`, `Instructor`, or `Administrator`.
- **CourseCategory:** normalized administrator-maintained category.
- **Course:** instructor-owned offering, category, details, price, thumbnail path, lifecycle status, and latest review metadata.
- **Enrollment:** Student-to-Course join entity with status and dates.
- **Payment:** an enrollment payment attempt with provider reference, status, gross amount, commission snapshot, instructor net amount, currency, and timestamps.
- **Invoice:** immutable receipt linked one-to-one with a successful payment, with invoice number and billing snapshots.

```text
User (Instructor)      1 ----- * Course
CourseCategory         1 ----- * Course
User (Student)         1 ----- * Enrollment * ----- 1 Course
Enrollment             1 ----- * Payment
Payment                1 ----- 0..1 Invoice
User (Administrator)   1 ----- * reviewed Course records
```

Use restricted deletion where records contribute to enrollment, review, payment, or invoice history. Use data annotations by default; use minimal Fluent API only for composite uniqueness or delete behavior that annotations cannot express.

### Validation Baseline

- Use form-specific ViewModels instead of binding entities directly.
- Require a normalized, unique course code and sensible title/description lengths.
- Require an existing active category.
- Price must be non-negative, within a documented maximum, and have at most two decimal places.
- Validate thumbnail type and size, generate its filename, and store only a relative path.
- Require a rejection reason.
- Derive or recheck user IDs, ownership, price, and payment amount on the server.
- Recheck authorization and current database state for every state-changing action.

### Practical-to-Feature Map

| Course practical | Planned evidence |
|---|---|
| Database SELECT | Catalogue, course details, enrolled courses, and billing history |
| SELECT continued | Search, filters, sorting, paging, and summary reports |
| AJAX | Catalogue filtering or status interaction using partial-view HTML/JSON |
| INSERT/UPDATE/DELETE | Course/category maintenance and safe archive behavior |
| Many-to-many | Student-to-Course through Enrollment |
| Photo upload | Validated course thumbnail |
| Security | Manual cookie login/logout, roles, ownership checks, protected records |
| Email and DateTime | Approval/payment notices and invoice email; store UTC and display Malaysia time |

### Additional Feature Priority

After the complete local core flow works, add additional features in this order:

1. Stripe test-mode payment with server verification and idempotency.
2. AJAX catalogue filtering with a partial view.
3. Temporary login blocking after repeated failures.
4. Emailed invoice/payment receipt.
5. Small administrator/instructor charts based on real project data.

Booking, e-material, survey, event, and older REST-integration features remain deferred unless an approved team scope brings them in.

### Proposed Development Sequence

1. Approve business rules, roles, module ownership, and open decisions.
2. Produce the report module outline and entity diagram.
3. Implement manual cookie authentication and authorization helpers.
4. Define approved entities, ViewModels, and `DbContext`.
5. Create/inspect the initial migration and seed useful demonstration data.
6. Build instructor Course CRUD, thumbnail upload, validation, and archive behavior.
7. Build administrator category maintenance and course review.
8. Build public catalogue, details, search/filter/paging, and partial view.
9. Build free and paid enrollment flows.
10. Build verified payment completion, invoice creation, and billing history.
11. Add only the highest-value additional features that fit the remaining time.
12. Verify roles, ownership, validation, history preservation, and the end-to-end demo.

### Decisions Required Before Application Coding

- Has the tutor approved the project title?
- Who are the team members, which modules does each own, and who owns shared User/Security work?
- Accept or amend administrator approval before course publication.
- Should the role be named `Instructor`, `Tutor`, or are both needed by different modules?
- Should enrolled students retain access after a course is archived?
- Is Stripe test mode intended after local enrollment/billing works?
- Is the proposed 15% platform commission acceptable, or should another rate/model be used?
- Will email use a real test/SMTP service or only a demonstrable development implementation?

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
- EF Core SQL Server provider `10.0.11` referenced by the project
- EF Core Design package `10.0.11` referenced by the project
- project-local `dotnet-ef` tool `10.0.11` recorded in `dotnet-tools.json`
- Docker CLI and Docker Compose installed, but the Docker Desktop Linux engine was not running during the audit
- WSL 2 configured with Ubuntu as the default distribution

Observed setup gaps:

- package restore/build after the EF Core additions has not yet been confirmed
- no application `DbContext` is present
- no database connection string is present
- no migrations are present
- no `compose.yml` or `.devcontainer/` configuration is present; this is intentional for the current native Windows decision

## 6. Current Repository State

The repository currently contains the standard initial ASP.NET Core MVC template, including:

- `Online_Tuition_Systems.csproj`
- `Program.cs`
- `Controllers/HomeController.cs`
- the starter `Models`, `Views`, and `wwwroot` content

`Online_Tuition_Systems.csproj` references the EF Core SQL Server and Design packages at version `10.0.11`. The root `dotnet-tools.json` records project-local `dotnet-ef` version `10.0.11`. `Program.cs` currently registers MVC and the normal starter request pipeline, but no EF Core context, authentication, or application services.

Git audit notes:

- branch: `main`
- remote tracking: `origin/main`
- latest observed commit: `73eb359` (`chore: configure native Windows workspace`)
- local branch was one commit ahead of `origin/main` at the latest inspection
- `Online_Tuition_Systems.csproj` contains uncommitted EF Core package changes
- `dotnet-tools.json` was untracked at the latest inspection

Do not clean, discard, untrack, or commit any of these items automatically. The user must review and perform Git changes manually.

## 7. Completed Work

- Initial ASP.NET Core MVC template exists.
- Project target is .NET 10.
- Windows development environment was audited.
- Native Windows + VS Code + LocalDB was selected as the current environment direction.
- Git is available from the VS Code PowerShell terminal.
- The `MSSQLLocalDB` SQL Server Express instance is running and its connection was verified.
- A project `.gitignore` was added for .NET build output, local logs, IDE state, test output, and local secret overrides.
- Previously tracked `obj/` build output was removed from Git tracking and recorded in commit `73eb359`.
- EF Core SQL Server and Design `10.0.11` package references are present, pending build confirmation.
- Project-local `dotnet-ef` `10.0.11` is configured, pending build/tool-restore confirmation on another machine.
- Repository assistant rules were clarified in `AGENTS.md`.
- This project handoff record was created.

No application `DbContext`, entity model, application database, migration, seed implementation, authentication module, or business module has been confirmed complete.

## 8. Immediate Next Steps

1. Review and approve or amend Section 2A.
2. Confirm project-title approval, team/module ownership, final roles, and the remaining open decisions.
3. Convert the approved baseline into the report module outline and entity diagram.
4. Manually run `dotnet restore`, `dotnet tool restore`, and `dotnet build` in the native Windows VS Code terminal.
5. Change Section 2A from proposed to confirmed before application implementation begins.
6. Configure `DbContext` and prepare migrations only after the data model is confirmed.

## 9. Planned Major Sections

These sections are planned but not yet confirmed complete:

1. Requirements recovery and Laravel-to-ASP.NET behavior mapping
2. System module outline, ownership, entity diagram, and monetization report
3. Billing and Course domain requirements and entity design
4. EF Core context, SQL Server connection, seed strategy, and initial migration
5. Manual cookie authentication and role authorization
6. Billing and Course implementation
7. Remaining core modules and integrated additional features
8. Validation, security review, practical-technique coverage, and end-to-end verification
9. Final SQL Server Express file/database preparation
10. Submission review and documentation

Module ownership and detailed business scope have not yet been recorded. The next source to inspect is the user's previous Laravel project and any associated requirements or database design.

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

### 2026-09-08 — Assignment and conversion planning

- Reviewed the supplied BMIT2023 marking rubric and assignment requirement text.
- Confirmed the report, architecture, presentation, validation/data-layer, security, core-module, and additional-feature marking priorities.
- Recorded the eight course practical areas that the system should demonstrate where relevant.
- Confirmed that the existing Laravel system will be used to recover reusable requirements and behavior, not translated line by line.
- Set Billing and Course as the first module priorities; their detailed scope remains pending review of the previous Laravel project.
- Deferred entity creation and the initial migration until requirements, ownership, business rules, and relationships are approved.

### 2026-09-08 — Laravel reference review and proposed ASP.NET design

- Read the temporary Laravel reference project's `AGENTS.md` and complete `PROJECT.md`.
- Inspected relevant Course, Enrollment, Payment, Invoice, policy, validation, service, controller, and migration code without modifying the reference.
- Treated conflicting historical Laravel workflows as context, not new-project requirements.
- Proposed a focused Course-to-Enrollment-to-Payment-to-Invoice workflow with administrator course approval.
- Proposed User, CourseCategory, Course, Enrollment, Payment, and Invoice as the core entities.
- Mapped all eight course practical areas to concrete features.
- Added a proposed 15% paid-course commission with transaction snapshots and deferred payouts to support the rubric's monetization requirement without adding a full accounting module.
- Deferred Stripe and old REST-integration features until the local core flow works and is approved.
- Excluded `Anywhere-Edureach/` from Git because it is a temporary reference, not part of the deliverable.
- Made no application-code, entity, migration, package, or database change; Section 2A awaits user review.
