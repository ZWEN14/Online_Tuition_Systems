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
- Project-title tutor approval: confirmed by the user on 2026-09-08

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
- the project title should be tutor-approved; the user confirmed approval on 2026-09-08.

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

## 2A. Confirmed Business Baseline

The user approved the core direction on 2026-09-08. Remaining team integration and report decisions must still be coordinated before shared entities are finalized.

### Design Goal and Main Flow

The simplest coherent marketplace flow is:

1. A tutor creates a draft course and submits it for review.
2. An administrator approves or rejects it with a reason.
3. An approved course is published in the catalogue.
4. A student enrolls immediately if it is free, or pays if it is paid.
5. A verified successful payment activates enrollment and creates an invoice.
6. The student accesses enrolled courses and their own billing history.

Administrator review before publication is confirmed. It gives the Administrator role a real business purpose, provides strong authorization evidence for the rubric, and prevents tutors from publishing unsuitable courses directly.

### Actors and Responsibilities

- **Guest:** browse/search published courses, view details, register, and log in.
- **Student:** enroll only in published courses; access only their own enrollments, payments, and invoices.
- **Tutor:** create and edit their own draft/rejected courses, upload a thumbnail, submit for review, see enrollment summaries, and archive eligible courses.
- **Administrator:** maintain categories; approve, reject, suspend, restore, or archive courses; view system-wide course and billing summaries. Administrators must not rewrite historical payment facts.

Use `Tutor` as the shared system role name for consistency with the likely team convention. In the Mentor–Mentee module, a Tutor participates as the mentor and a Student participates as the mentee. `Mentor` and `Mentee` are relationship/UI terms, not duplicate authentication roles, unless the team later identifies a genuinely different actor.

Use course-required manual cookie authentication, not ASP.NET Core Identity. Authorization must combine role checks with record ownership checks.

### Course Lifecycle

```text
Draft -> PendingReview -> Published -> Archived
                       -> Rejected -> edited Draft -> PendingReview
Published/Archived <-> Suspended (Administrator action)
```

- The authenticated tutor determines ownership; never trust a posted owner ID.
- Only an administrator can approve or reject a submitted course.
- Rejection requires a reason.
- Only published courses appear publicly or accept new enrollments.
- Archive instead of physically deleting a course that has enrollment or billing history.
- `Archived` means retired from the catalogue: it accepts no new enrollments, but existing active students retain access.
- `Suspended` means blocked by an Administrator for moderation/security reasons: it is hidden and course access is disabled until restored.
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
- Payment and invoice snapshots preserve historical course title, gross amount, platform fee, tutor net amount, payer details, and timestamps.
- Enrollment, payment, and invoice history must not be cascade-deleted.
- Checkout covers one course at a time.
- Exclude cart, subscriptions, tax, multiple currencies, installments, automated tutor payouts, coupons, and refunds from the baseline.

### Proposed Monetization Model

- The primary model is a percentage platform commission on each successful paid-course sale.
- The confirmed baseline rate is **15%** of the final amount paid after discounts.
- Store the commission rate, platform-fee amount, and tutor-net amount as transaction snapshots so later rate changes do not rewrite history.
- Free courses generate no direct revenue but can attract students and demonstrate the free-enrollment branch.
- Automated tutor payouts are outside the implementation baseline; the system only records the amount owed for reporting.
- The assignment report should compare at least two alternatives, such as commission versus tutor subscription, then justify commission as the selected model and provide conservative/base/optimistic revenue estimates using explicit assumptions.

### Promotion Scope

- Complete normal-price enrollment, payment, invoice, and 15% commission first.
- Add promotions afterward as an integrated Billing additional feature.
- Keep the first promotion design course-specific: a code, percentage discount, UTC start/end dates, active flag, and optional redemption limit.
- A Tutor may manage promotions only for their own published courses; an Administrator may disable an invalid promotion.
- Validate the promotion again on the server during checkout. Never accept a discount amount calculated by the browser.
- Calculate `FinalAmount = OriginalPrice - DiscountAmount`, then calculate the 15% platform fee from `FinalAmount`.
- Snapshot the promotion code, original price, discount, final amount, commission rate/amount, and tutor net amount on the completed transaction.
- Exclude promotion stacking and complex platform-wide campaigns from the initial version.

### Proposed Core Entities

- **User:** login/display data and one role: `Student`, `Tutor`, or `Administrator`.
- **CourseCategory:** normalized administrator-maintained category.
- **Course:** tutor-owned offering, category, details, price, thumbnail path, lifecycle status, and latest review metadata.
- **Enrollment:** Student-to-Course join entity with status and dates.
- **Payment:** an enrollment payment attempt with provider reference, status, gross amount, commission snapshot, tutor net amount, currency, and timestamps.
- **Invoice:** immutable receipt linked one-to-one with a successful payment, with invoice number and billing snapshots.

```text
User (Tutor)           1 ----- * Course
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

1. Stripe-hosted Checkout in a Stripe sandbox, using return-page server verification and idempotency.
2. Course-specific promotion codes after normal-price Billing is complete.
3. AJAX catalogue filtering with a partial view.
4. Temporary login blocking after repeated failures.
5. Emailed invoice/payment receipt.
6. Small administrator/tutor charts based on real project data.

For the assignment implementation, Stripe will use its hosted Checkout page in a sandbox and redirect back with the Checkout Session ID. The server must retrieve and verify the Session status, payment status, amount, currency, metadata, and ownership before activating enrollment. The return action must be idempotent. The user has chosen not to implement a webhook initially. This is acceptable for a classroom demonstration but is not production-complete because a customer might pay and never reach the return page; add a webhook later if the tutor requires production reliability.

Booking, e-material, survey, event, and older REST-integration features remain deferred unless an approved team scope brings them in.

### Proposed Development Sequence

1. Coordinate the confirmed business rules, shared role names, and User/Security ownership with the team.
2. Produce the report module outline and entity diagram.
3. Implement manual cookie authentication and authorization helpers.
4. Define approved entities, ViewModels, and `DbContext`.
5. Create/inspect the initial migration and seed useful demonstration data.
6. Build tutor Course CRUD, thumbnail upload, validation, and archive behavior.
7. Build administrator category maintenance and course review.
8. Build public catalogue, details, search/filter/paging, and partial view.
9. Build free and paid enrollment flows.
10. Build verified payment completion, invoice creation, and billing history.
11. Add only the highest-value additional features that fit the remaining time.
12. Verify roles, ownership, validation, history preservation, and the end-to-end demo.

### Team Scope and Ownership Boundaries

- **Current user's modules:** Course and Billing, including Enrollment, Payment, Invoice, commission, and later promotions.
- **Other team scope reported:** Mentor–Mentee; Event and Announcement; Survey and Complaint.
- User, manual cookie authentication, roles, navigation, notifications, and shared layout affect multiple modules and require agreed shared ownership/interfaces.
- Course/Billing must not implement the internal business logic of teammates' modules.
- Shared foreign keys should reference the common User entity rather than creating module-specific duplicate user tables.

### Confirmed Shared Foundation Scope

- The tutor has approved the Online Tuition System project title.
- The team agrees to shared `Tutor` and `Student` role names; mentor and mentee are relationship terms.
- This Course/Billing work may also implement the minimum shared User/Security foundation needed for the system: registration, login, logout, password hashing, manual cookie authentication, role authorization, ownership checks, and basic temporary login blocking if time permits.
- Keep shared security small and reusable. Do not absorb teammates' module-specific authorization or workflows.
- Provide one small shared email abstraction so other modules can request email without duplicating SMTP code.
- Email will use configurable SMTP aimed at a development/testing inbox. This is workable for demonstration but not a production mail deployment.
- Initial email use cases are course-review results and successful-payment/invoice notices.
- SMTP host/user/password values are local secrets and must never be committed. Committed configuration may contain only safe setting names and non-secret defaults.
- Advanced email queues, marketing campaigns, delivery tracking, and production mail infrastructure are out of scope.

## 2B. Confirmed Modular-Monolith Architecture

Use one ASP.NET Core MVC application and one SQL Server database, organized into clear internal modules. Do not create separate web applications, databases, microservices, or deployable projects for Course and Billing.

### Module Boundaries

| Module | Owns |
|---|---|
| Shared foundation | User, roles, manual cookie authentication, common navigation/layout, email abstraction, and `ApplicationDbContext` |
| Course Management | CourseCategory, Course, Tutor Course CRUD, Admin review/moderation, public catalogue, Enrollment, and course-access decisions |
| Billing | Payment attempts, Invoice, 15% commission, checkout, payment verification, billing history, and later promotions |
| Teammate modules | Mentor–Mentee, Event/Announcement, and Survey/Complaint business logic |

Course and Billing are separate modules but form one integrated workflow. Course Management decides whether a Student may enroll and creates the Enrollment. Billing handles payment for a pending paid Enrollment. A verified successful payment activates that Enrollment and creates one Invoice in a single database transaction.

### Folder and Code Convention

- Keep the existing single project and conventional MVC routing.
- Group module-specific controllers, ViewModels, services, and views by Course or Billing using clear folders/namespaces.
- Keep shared EF entities in `Models/` and the single context in `Data/` so teammate modules can reference common User and Course records.
- Put business rules in focused Course/Billing services; keep controllers thin and use form/list/detail ViewModels for the UI.
- Use service interfaces only at real cross-module boundaries, especially payment completion and enrollment activation.
- Do not add a generic repository layer, mediator framework, separate class libraries, or microservices unless the project later has a concrete need.
- Teammate modules must reference shared User/Course keys rather than duplicate those tables or copy Course/Billing logic.

### External Reference and Copying Policy

- Older ASP.NET Core 5 GitHub projects may be used to understand page flows, business rules, naming, and UI ideas.
- Do not copy their startup, authentication, EF configuration, package setup, or framework-specific code directly into .NET 10.
- Check the repository licence before copying any actual code. No licence means there is no automatic permission to copy it.
- Preserve any attribution or notice required by a permissive licence and follow the institution's academic-integrity rules even when a licence allows reuse.
- Prefer reimplementation in the project's own conventions because the rubric rewards project-owned work and understanding.
- Review candidate GitHub links individually for licence, security, .NET 10 compatibility, and relevance before adopting code.

### GitHub Reference Review — 2026-09-08

Two user-supplied repositories were reviewed:

- `MirazMuhammod/course-management-system-api` is a .NET 9 Web API organized into API, business-access, and data-access projects. It uses JWT, DTOs, services, AutoMapper-style mapping, and repository abstractions. Useful ideas are category validation, dedicated input models, async Course services, and separating enrollment behavior from Course CRUD.
- `pacheco4480/SchoolManagementSystem` is a .NET 8 MVC school administration system using ASP.NET Core Identity, repository/converter helpers, Syncfusion, Azure storage, MailKit, JWT, and EF preview dependencies. Useful ideas are responsive Course CRUD pages, dedicated Course ViewModels, role-based navigation, and friendly not-found/error handling.

Neither repository declares a repository-level licence in its current GitHub metadata/tree. Their source code must not be copied into this project. Only general ideas and business/UI patterns may be independently reimplemented. Identity, JWT, preview packages, Syncfusion, Azure storage, multi-project layering, and generic repository infrastructure do not fit the assignment or the confirmed simple modular-monolith design.

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

- the user reported successful package restore and build after the EF Core/domain additions
- `ApplicationDbContext` registration and the project-local LocalDB connection build successfully
- the LocalDB connection still awaits runtime/migration verification
- no migrations are present
- no `compose.yml` or `.devcontainer/` configuration is present; this is intentional for the current native Windows decision

## 6. Current Repository State

The repository currently contains the standard initial ASP.NET Core MVC template, including:

- `Online_Tuition_Systems.csproj`
- `Program.cs`
- `Controllers/HomeController.cs`
- the starter `Models`, `Views`, and `wwwroot` content

`Online_Tuition_Systems.csproj` references the EF Core SQL Server and Design packages at version `10.0.11`. The root `dotnet-tools.json` records project-local `dotnet-ef` version `10.0.11`. `Program.cs` registers MVC and `ApplicationDbContext`, but does not yet register authentication or application services.

Current development phase: Course Management Part 1 source is implemented and awaits watcher/runtime verification: public catalogue/details plus Tutor-owned draft creation and listing. Database-backed functions still await the first migration. No migration or database update has been authorized yet.

Current unverified domain source:

- `Models/DomainEnums.cs`: shared User, Course, Enrollment, and Payment statuses;
- `Models/User.cs`: shared Student/Tutor/Administrator user foundation;
- `Models/CourseCategory.cs` and `Models/Course.cs`: category, Tutor ownership, Admin review, lifecycle, pricing, and course history;
- `Models/Enrollment.cs`: the unique Student-to-Course join entity;
- `Models/Payment.cs` and `Models/Invoice.cs`: payment attempts, immutable transaction snapshots, 15% commission fields, and one-invoice-per-successful-payment structure;
- `Data/ApplicationDbContext.cs`: entity sets and restricted-delete relationships.

Current unverified LocalDB configuration:

- `Program.cs` sets `App_Data` as `|DataDirectory|` and registers `ApplicationDbContext` with the SQL Server provider;
- `appsettings.json` contains a Windows-authenticated `MSSQLLocalDB` connection targeting `App_Data/OnlineTuitionSystems.mdf`;
- `.gitignore` excludes generated database files while retaining `App_Data/.gitkeep`;
- the configuration does not create a database until the user later runs an authorized EF Core database command.

Current authentication source:

- `Services/Security/IPasswordHasher.cs` and `Pbkdf2PasswordHasher.cs`: project-owned salted PBKDF2-SHA256 password hashing and fixed-time verification without ASP.NET Core Identity;
- `ViewModels/Account/LoginViewModel.cs` and `RegisterViewModel.cs`: form-specific validation, password confirmation, and Student/Tutor selection;
- `Controllers/AccountController.cs`: registration, normalized unique email checks, login, local return URLs, role claims, logout, and access-denied handling;
- `Views/Account/`: Bootstrap login, registration, and access-denied pages with client-side validation hooks;
- `Program.cs`: cookie authentication/authorization registration and correctly ordered authentication middleware;
- `Views/Shared/_Layout.cshtml`: authenticated-user greeting and POST logout navigation.

The user confirmed that `Account/Register` renders while using `dotnet watch`. Registration submission, persisted login, logout, and role authorization are not yet runtime-verified because the application database does not exist.

Current unverified Course Management Part 1 source:

- `Services/Courses/CourseService.cs`: published catalogue search/category filtering/paging, public details, Tutor-owned listing, active-category checks, normalized unique codes, unique slugs, and draft creation;
- `Services/Courses/LocalCourseImageStorage.cs`: generated local thumbnail filenames and constrained project-local storage/deletion;
- `Validation/CourseImageAttribute.cs`: custom JPG/PNG/WebP and 2 MB server-side upload validation;
- `ViewModels/Courses/`: separate catalogue, card, details, Tutor-list, category-option, and create-form models;
- `Controllers/CoursesController.cs`: public catalogue and published-course details;
- `Controllers/TutorCoursesController.cs`: role-protected Tutor list/create actions deriving ownership from the authenticated user claim;
- `Views/Courses/` and `Views/TutorCourses/`: responsive catalogue/details and Tutor list/create pages;
- `Program.cs` and the shared layout: Course services plus public and Tutor navigation.

Git audit notes:

- current branch: `feature/Billing-Course`
- remote tracking: `origin/main`
- latest observed commit: `1b7d23e` (`Project Setup`)
- `main` and `origin/main` point to `18445de`; `main` is an ancestor of the feature branch
- the feature branch is two commits ahead and zero commits behind `main`; this is not a two-sided divergence
- the working tree was clean at the latest inspection before this documentation update
- `.gitignore`, `PROJECT.md`, the EF Core package references, and `dotnet-tools.json` are present in feature-branch history

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

The user confirmed that the domain entity, `DbContext`, LocalDB registration, and authentication source compile through `dotnet watch`, and that the registration page renders. Authentication remains only partially verified until database-backed registration/login work. No application database, migration, seed implementation, Course/Billing controller workflow, or business module has been confirmed complete.

## 8. Immediate Next Steps

1. Review any `dotnet watch` compiler feedback from Course Management Part 1.
2. Review the shared User/Course keys with teammates before schema creation.
3. Prepare and inspect the first migration.
4. Apply the migration only through a separate explicit user-authorized database step.
5. Verify registration/login, public catalogue, Tutor listing, validation, and draft creation against LocalDB.
6. Implement Course Management Part 2: edit, submit for review, Admin approval/rejection, archive, and suspension.

The normal development verification loop is the user's existing `dotnet watch`. Do not repeatedly ask for a separate `dotnet build`; ask only for watcher/compiler errors or targeted runtime results when needed.

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

Course/Billing scope and the other team module areas are now recorded. Shared User/Security ownership and cross-module relationship details still require team coordination before the common data model is finalized.

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
- Made no application-code, entity, migration, package, or database change; Section 2A was awaiting user review at that point.

### 2026-09-08 — Core business decisions and feature-branch verification

- Confirmed Administrator approval before course publication.
- Initially proposed `Instructor` as the Course-module role; this was superseded by the `Tutor` convention in the next decision.
- Defined `Archived` as retired from new sales while preserving enrolled-student access, and `Suspended` as an Administrator block that disables access.
- Confirmed a 15% platform commission calculated after discounts.
- Scoped course-specific promotion codes after the normal-price core Billing flow.
- Chose Stripe-hosted Checkout in a sandbox with server verification on return and no initial webhook, while recording the reliability limitation.
- Recorded the current user's Course/Billing ownership and the team's Mentor–Mentee, Event/Announcement, and Survey/Complaint areas.
- Verified that the earlier `PROJECT.md` planning, `.gitignore`, EF Core references, and local tool manifest remain in `feature/Billing-Course` history.
- Verified that the feature branch is two commits ahead and zero behind `main`; no application code or Git state was changed during this review.

### 2026-09-08 — Tutor convention and domain-foundation start

- Confirmed `Tutor` instead of `Instructor` as the shared role name for consistency across team modules.
- Defined Tutor as the mentor-side user and Student as the mentee-side user; mentor/mentee remain relationship terms.
- Authorized the Course/Billing implementation to begin by adapting useful Anywhere-Edureach rules and structure to ASP.NET Core MVC.
- Started the domain-foundation section; migrations and database changes remain deferred until the entity model is reviewed.

### 2026-09-08 — Domain-foundation source added, awaiting verification

- Added typed roles/statuses and initial User, CourseCategory, Course, Enrollment, Payment, and Invoice entities.
- Used data annotations for table, column, validation, and index definitions.
- Added only the necessary relationship configuration for unambiguous User relationships and restricted historical-record deletion.
- Preserved the Laravel reference's useful course/enrollment/payment/invoice structure while adding Admin review, suspension, commission, discount snapshots, and Tutor naming.
- Did not add promotions as a separate entity yet; normal-price Billing remains the first implementation target.
- Added `ApplicationDbContext` source without registering a connection, creating a migration, or changing the database.
- This section is implemented but not complete until the user confirms a successful manual build and reviews the shared User model.

### 2026-09-08 — Shared foundation decisions confirmed

- Recorded that the tutor approved the Online Tuition System title.
- Confirmed shared `Tutor`/`Student` naming with mentor/mentee used as relationship terminology.
- Accepted responsibility for a minimal reusable User/Security foundation if needed, without taking ownership of teammates' module-specific logic.
- Selected configurable SMTP to a development/testing inbox for workable course-review and invoice email demonstrations.
- Kept SMTP credentials outside committed project configuration and excluded production mail infrastructure from scope.

### 2026-09-08 — Domain foundation build confirmed

- The user reported completing the requested restore and build successfully.
- Marked the Course/Billing entity and `ApplicationDbContext` source foundation complete.
- Started the LocalDB context-registration and connection-configuration section.
- No migration or database update was run.

### 2026-09-08 — LocalDB context configuration added, awaiting verification

- Registered `ApplicationDbContext` with EF Core SQL Server in `Program.cs`.
- Set the project `App_Data` directory as the `|DataDirectory|` value used by LocalDB.
- Added a Windows-authenticated LocalDB connection for `OnlineTuitionSystems.mdf` with no embedded credentials.
- Added `App_Data/.gitkeep` and ignored generated LocalDB data/log files in Git.
- Validated the configuration file as JSON and found no whitespace errors in the current diff.
- Did not create a migration, database, `.mdf`, or `.ldf`; the section awaits the user's manual build confirmation.

### 2026-09-08 — LocalDB build confirmed and authentication started

- The user indicated that the LocalDB context-registration build completed and requested the next section.
- Marked the LocalDB source configuration build-verified while leaving runtime/database verification pending.
- Started the minimum manual cookie-authentication section.
- No migration or database update was run.

### 2026-09-08 — Manual cookie-authentication source added, awaiting verification

- Added project-owned PBKDF2-SHA256 password hashing without ASP.NET Core Identity or another package.
- Added validated Student/Tutor registration; public registration cannot create an Administrator.
- Added login with generic failure messages, active-account checks, safe local return URLs, and cookie claims for ID, name, email, role, and institution ID.
- Added protected POST logout, access-denied handling, Account Razor views, and authentication-aware shared navigation.
- Registered cookie authentication and placed authentication middleware before authorization.
- Did not create or change the database; registration/login cannot run until the first migration is later created and applied.
- The authentication section remains unverified until the user confirms a successful manual build.

### 2026-09-08 — Modular-monolith architecture confirmed

- Confirmed one integrated MVC application and database with separate internal Course Management and Billing modules.
- Assigned Enrollment and access decisions to Course Management; Billing owns checkout, payments, invoices, commission, and later promotions.
- Chose conventional module folders and focused services instead of separate applications, microservices, generic repositories, or additional architecture frameworks.
- Recorded safe reuse rules for older ASP.NET Core GitHub references: check licences, reuse ideas selectively, and reimplement obsolete framework/security code for .NET 10.
- Confirmed that `dotnet watch` compiles the current source and that the Account/Register page renders.
- Recorded that database-backed authentication remains unverified until the first migration is created and applied.
- Stopped requesting a separate `dotnet build` as part of the normal loop; future verification will use watcher/compiler output and targeted browser checks.

### 2026-09-08 — GitHub references assessed and Course Management started

- Reviewed the two user-supplied GitHub repositories at the architecture, dependency, entity, ViewModel, controller, and service level.
- Found no repository-level licence for either project, so no source code will be copied.
- Selected only general ideas to reimplement: dedicated form/list/detail models, active-category validation, async services, thin controllers, protected management pages, and friendly error handling.
- Rejected incompatible Identity/JWT security, preview dependencies, paid UI/storage dependencies, generic repositories, and unnecessary multi-project layering.
- Started Course Management Part 1 within the confirmed modular monolith.

### 2026-09-08 — Course Management Part 1 source added, awaiting verification

- Added public published-course catalogue, search, category filter, paging, details, and reusable course-card partial view.
- Added protected Tutor-owned Course listing and draft-creation flow.
- Added dedicated Course ViewModels with data-annotation validation and active-category/code checks in the Course service.
- Added custom thumbnail validation and randomized local image storage under `wwwroot/uploads/courses`.
- Derived Course ownership from the authenticated Tutor claim rather than form input.
- Registered focused Course services and added public/Tutor navigation without adding repository or mapping frameworks.
- Did not copy source from either unlicensed GitHub repository; only general patterns were independently implemented.
- Did not create or apply a migration or change the database.
- Part 1 remains incomplete until watcher/compiler feedback is clear and the database-backed pages are verified.

### 2026-09-08 — Course catalogue Razor pagination fix

- Corrected the Razor compilation failure in `Views/Courses/Index.cshtml` by renaming the pagination loop variable from `page` to `pageNumber`; Razor interpreted `@page` as the Razor Pages directive.
- Course Management Part 1 remains awaiting watcher/compiler and browser verification.

### 2026-09-08 — Initial migration created; LocalDB path source corrected

- The user created the `InitialCreate` migration successfully.
- The first database update failed because the EF Core design-time process expanded `|DataDirectory|` to `C:\` and SQL Server was denied access to create `C:\OnlineTuitionSystems.mdf`.
- Updated `Program.cs` to resolve `|DataDirectory|` explicitly to the repository's absolute `App_Data` path before supplying the connection string to EF Core.
- At this point the migration remained unapplied; the later LocalDB confirmation records its successful application.

### 2026-09-08 — Initial LocalDB database confirmed; Course workflow Part 2 started

- The user confirmed that the database update succeeded and `/Courses` now loads against LocalDB.
- The empty public catalogue is expected because only approved `Published` courses are shown and the new database has no application data yet.
- Started the minimum end-to-end Course workflow: one-time Administrator setup, category creation, Tutor submission, and Administrator approval.
- Visual sizing and heading refinements are deferred until the core workflow is functional.

### 2026-09-08 — Course approval workflow source added, awaiting verification

- Added a one-time first-user Administrator setup page; it becomes unavailable after any user exists.
- Added Administrator-only course-category creation and active/inactive management.
- Added Tutor-owned draft submission with valid status, ownership, active-account, and active-category checks.
- Added Administrator-only pending review, approval/publication, and rejection-with-reason actions.
- Added role-aware navigation and Tutor feedback for success, errors, and rejection reasons.
- Added no entity or schema changes, so no additional migration is required for this section.
- The workflow remains incomplete until the user verifies it through the existing watcher and browser.

### 2026-09-08 — Runtime upload watcher failure diagnosed

- A Tutor thumbnail upload successfully created the image, but `dotnet watch` then crashed inside `HotReloadMSBuildWorkspace` while processing the new runtime file under `wwwroot/uploads`.
- Excluded `App_Data` and `wwwroot/uploads` from the .NET 10 default watch item set without excluding normal source/static assets.
- Added runtime uploads to `.gitignore`; the existing uploaded image and database record were preserved.
- Watcher stability and the saved draft remain awaiting user verification after restarting `dotnet watch`.

### 2026-09-08 — Core Course publication workflow verified

- The user confirmed the complete browser flow works: initial Administrator setup, category creation, Tutor registration, draft creation with thumbnail upload, review submission, Administrator approval, and public catalogue display.
- Confirmed the LocalDB database, manual authentication roles, ownership-based Tutor actions, image upload, review transition, and published-course query work together end to end.
- Marked Course Management Part 1 and the minimum Administrator approval workflow complete.
- Deferred heading/size visual refinements until functional module work is complete.
- The next Course section is lifecycle completion: Tutor editing and archiving plus Administrator suspension/restoration; no schema change is expected.

### 2026-09-08 — Course lifecycle completion source added, awaiting verification

- Added Tutor editing for owned Draft and Rejected courses, including category/code revalidation and safe thumbnail replacement/removal.
- Editing a Rejected course returns it to Draft and clears the previous review decision before resubmission.
- Added Tutor archiving for owned Published courses; archived courses disappear from new public sales while historical access rules remain reserved for Enrollment work.
- Added Administrator management across all courses with reason-required suspension and restoration to the prior Published or Archived status.
- Preserved role, ownership, active-category, and valid status-transition checks in the service layer.
- Added no schema changes or migration; lifecycle behavior remains awaiting watcher and browser verification.

### 2026-09-10 — Authentication ownership and integration timing clarified

- The teammate owns the final login/security module; the current project-owned cookie login is a temporary functional simulator for Course/Billing development and verification.
- Do not expand the temporary authentication implementation with nonessential login features.
- Agree on the shared contract now: User primary key, `Student`/`Tutor`/`Administrator` role names, active-account behavior, and the ID/role claims consumed by Course/Billing authorization.
- After the current Course lifecycle verification checkpoint, inspect and integrate the teammate's stable authentication/User implementation before expanding Enrollment and Billing. Waiting until every MVC module is finished would increase entity, foreign-key, migration, and authorization conflicts.
- Integration should replace the temporary Account implementation while preserving Course/Billing ownership and role checks; do not copy or merge blindly before reviewing both models and migrations.
- Forgotten local demo credentials may be handled by dropping and recreating the development LocalDB database from the existing `InitialCreate` migration. The migration files themselves do not need to be recreated.
