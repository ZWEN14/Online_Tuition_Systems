-- Demonstration data for Announcements, Events, Event Proposals, Registrations, and Notifications.
-- Safe to rerun: records are keyed by the "DEMO - " title prefix and refreshed in place.
-- Prerequisites:
--   1. Run Database/AddDemoUsers.sql
--   2. Run Database/AddDemoCourses.sql
USE [OnlineTuitionDb];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @StudentId int =
(
    SELECT TOP (1) Id
    FROM dbo.Users
    WHERE Email = N'student@test.com' AND Role = 0 AND IsBlocked = 0 AND EmailVerified = 1
    ORDER BY Id
);

DECLARE @TutorId int =
(
    SELECT TOP (1) Id
    FROM dbo.Users
    WHERE Email = N'tutor@test.com' AND Role = 1 AND IsBlocked = 0 AND EmailVerified = 1
    ORDER BY Id
);

DECLARE @AdminId int =
(
    SELECT TOP (1) Id
    FROM dbo.Users
    WHERE Email = N'admin@test.com' AND Role = 2 AND IsBlocked = 0 AND EmailVerified = 1
    ORDER BY Id
);

IF @StudentId IS NULL
BEGIN
    THROW 50001, 'Create or verify student@test.com before seeding announcement and event demo data.', 1;
END;

IF @TutorId IS NULL
BEGIN
    THROW 50002, 'Create or verify tutor@test.com before seeding announcement and event demo data.', 1;
END;

IF @AdminId IS NULL
BEGIN
    THROW 50003, 'Create or verify admin@test.com before seeding announcement and event demo data.', 1;
END;

DECLARE @TutorRef nvarchar(450) = CONVERT(nvarchar(450), @TutorId);
DECLARE @AdminRef nvarchar(450) = CONVERT(nvarchar(450), @AdminId);
DECLARE @StudentRef nvarchar(450) = CONVERT(nvarchar(450), @StudentId);
DECLARE @NowUtc datetimeoffset = SYSUTCDATETIME();

DECLARE @CourseCSharp int =
(
    SELECT TOP (1) CourseId
    FROM dbo.Courses
    WHERE Code = N'DEMO-CSHARP-101' AND Status = 4
);

DECLARE @CourseMvc int =
(
    SELECT TOP (1) CourseId
    FROM dbo.Courses
    WHERE Code = N'DEMO-MVC-101' AND Status = 4
);

DECLARE @CoursePython int =
(
    SELECT TOP (1) CourseId
    FROM dbo.Courses
    WHERE Code = N'DEMO-PYTHON-101' AND Status = 4
);

DECLARE @CourseAjax int =
(
    SELECT TOP (1) CourseId
    FROM dbo.Courses
    WHERE Code = N'DEMO-AJAX-120' AND Status = 4
);

DECLARE @CourseEfCore int =
(
    SELECT TOP (1) CourseId
    FROM dbo.Courses
    WHERE Code = N'DEMO-EFCORE-201' AND Status = 4
);

IF @CourseCSharp IS NULL OR @CourseMvc IS NULL OR @CoursePython IS NULL OR @CourseAjax IS NULL OR @CourseEfCore IS NULL
BEGIN
    THROW 50004, 'Run Database/AddDemoCourses.sql first so the required DEMO courses exist.', 1;
END;

DECLARE @InsertedAnnouncements int = 0;
DECLARE @UpdatedAnnouncements int = 0;
DECLARE @InsertedEvents int = 0;
DECLARE @UpdatedEvents int = 0;
DECLARE @InsertedProposals int = 0;
DECLARE @UpdatedProposals int = 0;
DECLARE @InsertedRegistrations int = 0;
DECLARE @UpdatedRegistrations int = 0;
DECLARE @InsertedNotifications int = 0;
DECLARE @InsertedEnrollments int = 0;

BEGIN TRANSACTION;

-- Active enrollments help course-scoped announcement and event notifications reach Students.
IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Enrollments
    WHERE StudentId = @StudentId AND CourseId = @CourseCSharp
)
BEGIN
    INSERT INTO dbo.Enrollments
        (StudentId, CourseId, Status, EnrolledAtUtc, ActivatedAtUtc)
    VALUES
        (@StudentId, @CourseCSharp, 2, @NowUtc, @NowUtc);

    SET @InsertedEnrollments += 1;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Enrollments
    WHERE StudentId = @StudentId AND CourseId = @CourseMvc
)
BEGIN
    INSERT INTO dbo.Enrollments
        (StudentId, CourseId, Status, EnrolledAtUtc, ActivatedAtUtc)
    VALUES
        (@StudentId, @CourseMvc, 2, @NowUtc, @NowUtc);

    SET @InsertedEnrollments += 1;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Enrollments
    WHERE StudentId = @StudentId AND CourseId = @CoursePython
)
BEGIN
    INSERT INTO dbo.Enrollments
        (StudentId, CourseId, Status, EnrolledAtUtc, ActivatedAtUtc)
    VALUES
        (@StudentId, @CoursePython, 2, @NowUtc, @NowUtc);

    SET @InsertedEnrollments += 1;
END;

DECLARE @DemoAnnouncements TABLE
(
    SeedKey nvarchar(80) NOT NULL PRIMARY KEY,
    CourseId int NULL,
    EventId int NULL,
    Title nvarchar(150) NOT NULL,
    Content nvarchar(max) NOT NULL,
    Audience nvarchar(20) NOT NULL,
    Priority nvarchar(20) NOT NULL,
    Status nvarchar(20) NOT NULL,
    PublishedAt datetimeoffset NULL,
    ExpiresAt datetimeoffset NULL,
    CreatedAt datetimeoffset NOT NULL
);

INSERT INTO @DemoAnnouncements
    (SeedKey, CourseId, EventId, Title, Content, Audience, Priority, Status, PublishedAt, ExpiresAt, CreatedAt)
VALUES
    (N'welcome', NULL, NULL,
     N'DEMO - Welcome to Online Tuition',
     N'This is a platform-wide welcome announcement for Students and Tutors. Browse courses, register for events, and check notifications regularly.',
     N'All', N'Normal', N'Published', DATEADD(day, -7, @NowUtc), NULL, DATEADD(day, -8, @NowUtc)),
    (N'maintenance', NULL, NULL,
     N'DEMO - Scheduled Maintenance Notice',
     N'The system will undergo brief maintenance this month. Save your work before the maintenance window and sign in again afterward if needed.',
     N'All', N'Urgent', N'Published', DATEADD(day, -2, @NowUtc), DATEADD(day, 30, @NowUtc), DATEADD(day, -3, @NowUtc)),
    (N'mvc-course', @CourseMvc, NULL,
     N'DEMO - MVC Assignment Brief Released',
     N'The ASP.NET Core MVC assignment brief is now available. Review controller actions, ViewModels, validation, and the project submission checklist.',
     N'Student', N'Important', N'Published', DATEADD(day, -1, @NowUtc), NULL, DATEADD(day, -2, @NowUtc)),
    (N'python-course', @CoursePython, NULL,
     N'DEMO - Python Lab Resources Updated',
     N'New starter files and sample exercises were added for the Python for Beginners course. Enrolled Students can review them before the next live session.',
     N'All', N'Normal', N'Published', DATEADD(day, -1, @NowUtc), NULL, DATEADD(day, -2, @NowUtc)),
    (N'draft', NULL, NULL,
     N'DEMO - Draft Exam Timetable',
     N'This draft announcement is kept unpublished so Administrators can demonstrate the publish workflow.',
     N'All', N'Normal', N'Draft', NULL, NULL, DATEADD(day, -1, @NowUtc)),
    (N'archived', NULL, NULL,
     N'DEMO - Archived Orientation Message',
     N'This archived announcement remains visible to Administrators for audit and history demonstrations.',
     N'All', N'Normal', N'Archived', DATEADD(day, -60, @NowUtc), DATEADD(day, -30, @NowUtc), DATEADD(day, -61, @NowUtc));

UPDATE existing
SET existing.CourseId = source.CourseId,
    existing.EventId = source.EventId,
    existing.CreatedByUserId = @AdminRef,
    existing.Title = source.Title,
    existing.Content = source.Content,
    existing.Audience = source.Audience,
    existing.Priority = source.Priority,
    existing.Status = source.Status,
    existing.PublishedAt = source.PublishedAt,
    existing.ExpiresAt = source.ExpiresAt,
    existing.UpdatedAt = @NowUtc
FROM dbo.Announcements AS existing
INNER JOIN @DemoAnnouncements AS source ON source.Title = existing.Title;

SET @UpdatedAnnouncements = @@ROWCOUNT;

INSERT INTO dbo.Announcements
(
    CourseId, EventId, CreatedByUserId, Title, Content,
    Audience, Priority, Status, PublishedAt, ExpiresAt, CreatedAt, UpdatedAt
)
SELECT
    source.CourseId,
    source.EventId,
    @AdminRef,
    source.Title,
    source.Content,
    source.Audience,
    source.Priority,
    source.Status,
    source.PublishedAt,
    source.ExpiresAt,
    source.CreatedAt,
    @NowUtc
FROM @DemoAnnouncements AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Announcements AS existing
    WHERE existing.Title = source.Title
);

SET @InsertedAnnouncements = @@ROWCOUNT;

DECLARE @DemoEvents TABLE
(
    SeedKey nvarchar(80) NOT NULL PRIMARY KEY,
    CourseId int NULL,
    Title nvarchar(150) NOT NULL,
    Description nvarchar(max) NOT NULL,
    StartsAt datetimeoffset NOT NULL,
    EndsAt datetimeoffset NOT NULL,
    ApplicationDeadline datetimeoffset NOT NULL,
    RegistrationAudience nvarchar(20) NOT NULL,
    Mode nvarchar(20) NOT NULL,
    Location nvarchar(255) NULL,
    MeetingPlatform nvarchar(30) NULL,
    MeetingUrl nvarchar(2048) NULL,
    MaxParticipants int NOT NULL,
    Status nvarchar(20) NOT NULL,
    CancellationReason nvarchar(2000) NULL,
    CancelledByUserId nvarchar(450) NULL,
    CancelledAt datetimeoffset NULL,
    CreatedAt datetimeoffset NOT NULL
);

INSERT INTO @DemoEvents
(
    SeedKey, CourseId, Title, Description, StartsAt, EndsAt, ApplicationDeadline,
    RegistrationAudience, Mode, Location, MeetingPlatform, MeetingUrl,
    MaxParticipants, Status, CancellationReason, CancelledByUserId, CancelledAt, CreatedAt
)
VALUES
    (N'mvc-workshop', @CourseMvc,
     N'DEMO - MVC Live Coding Workshop',
     N'A tutor-led online workshop covering controllers, Razor views, validation, and a guided mini feature build using ASP.NET Core MVC.',
     DATEADD(day, 14, @NowUtc), DATEADD(hour, 2, DATEADD(day, 14, @NowUtc)), DATEADD(day, 11, @NowUtc),
     N'All', N'Online', NULL, N'Zoom', N'https://zoom.us/j/demo-mvc-workshop', 40, N'Published', NULL, NULL, NULL, DATEADD(day, -10, @NowUtc)),
    (N'python-lab', @CoursePython,
     N'DEMO - Python Problem-Solving Lab',
     N'An in-person lab session for practising loops, functions, and small Python projects with tutor support.',
     DATEADD(day, 21, @NowUtc), DATEADD(hour, 3, DATEADD(day, 21, @NowUtc)), DATEADD(day, 18, @NowUtc),
     N'Student', N'Physical', N'BMIT Lab 2, Level 3', NULL, NULL, 25, N'Published', NULL, NULL, NULL, DATEADD(day, -9, @NowUtc)),
    (N'ajax-clinic', @CourseAjax,
     N'DEMO - AJAX Clinic and Q&A',
     N'An online clinic for fetch requests, JSON handling, progressive enhancement, and debugging AJAX catalogue interactions.',
     DATEADD(day, 28, @NowUtc), DATEADD(hour, 2, DATEADD(day, 28, @NowUtc)), DATEADD(day, 25, @NowUtc),
     N'Student', N'Online', NULL, N'GoogleMeet', N'https://meet.google.com/demo-ajax-clinic', 30, N'Published', NULL, NULL, NULL, DATEADD(day, -8, @NowUtc)),
    (N'efcore-seminar', @CourseEfCore,
     N'DEMO - EF Core and SQL Server Seminar',
     N'A hybrid seminar on code-first entities, relationships, migrations, and safe querying with Entity Framework Core.',
     DATEADD(day, 35, @NowUtc), DATEADD(hour, 2, DATEADD(day, 35, @NowUtc)), DATEADD(day, 32, @NowUtc),
     N'All', N'Hybrid', N'Library Seminar Room B', N'MicrosoftTeams', N'https://teams.microsoft.com/l/meetup-join/demo-efcore', 50, N'Published', NULL, NULL, NULL, DATEADD(day, -7, @NowUtc)),
    (N'cancelled-webinar', @CourseAjax,
     N'DEMO - Cancelled JavaScript Webinar',
     N'This cancelled event remains in the database to demonstrate event cancellation notifications and registration handling.',
     DATEADD(day, 10, @NowUtc), DATEADD(hour, 2, DATEADD(day, 10, @NowUtc)), DATEADD(day, 7, @NowUtc),
     N'All', N'Online', NULL, N'Zoom', N'https://zoom.us/j/demo-cancelled-webinar', 20, N'Cancelled',
     N'The tutor became unavailable and the session was rescheduled for a later semester.', @AdminRef, DATEADD(day, -1, @NowUtc), DATEADD(day, -12, @NowUtc)),
    (N'draft-from-proposal', @CourseEfCore,
     N'DEMO - Draft Database Design Clinic',
     N'A draft event created from an approved Tutor proposal. Administrators can publish it after final checks.',
     DATEADD(day, 42, @NowUtc), DATEADD(hour, 2, DATEADD(day, 42, @NowUtc)), DATEADD(day, 39, @NowUtc),
     N'Tutor', N'Online', NULL, N'Zoom', N'https://zoom.us/j/demo-db-clinic', 15, N'Draft', NULL, NULL, NULL, DATEADD(day, -4, @NowUtc));

UPDATE existing
SET existing.CourseId = source.CourseId,
    existing.CreatedByUserId = @AdminRef,
    existing.OrganizerUserId = @TutorRef,
    existing.Title = source.Title,
    existing.Description = source.Description,
    existing.StartsAt = source.StartsAt,
    existing.EndsAt = source.EndsAt,
    existing.ApplicationDeadline = source.ApplicationDeadline,
    existing.RegistrationAudience = source.RegistrationAudience,
    existing.Mode = source.Mode,
    existing.Location = source.Location,
    existing.MeetingPlatform = source.MeetingPlatform,
    existing.MeetingUrl = source.MeetingUrl,
    existing.MaxParticipants = source.MaxParticipants,
    existing.Status = source.Status,
    existing.CancellationReason = source.CancellationReason,
    existing.CancelledByUserId = source.CancelledByUserId,
    existing.CancelledAt = source.CancelledAt,
    existing.UpdatedAt = @NowUtc
FROM dbo.Events AS existing
INNER JOIN @DemoEvents AS source ON source.Title = existing.Title;

SET @UpdatedEvents = @@ROWCOUNT;

INSERT INTO dbo.Events
(
    CourseId, CreatedByUserId, OrganizerUserId, Title, Description,
    StartsAt, EndsAt, ApplicationDeadline, RegistrationAudience, Mode,
    Location, MeetingPlatform, MeetingUrl, MaxParticipants, Status,
    CancellationReason, CancelledByUserId, CancelledAt, CreatedAt, UpdatedAt
)
SELECT
    source.CourseId,
    @AdminRef,
    @TutorRef,
    source.Title,
    source.Description,
    source.StartsAt,
    source.EndsAt,
    source.ApplicationDeadline,
    source.RegistrationAudience,
    source.Mode,
    source.Location,
    source.MeetingPlatform,
    source.MeetingUrl,
    source.MaxParticipants,
    source.Status,
    source.CancellationReason,
    source.CancelledByUserId,
    source.CancelledAt,
    source.CreatedAt,
    @NowUtc
FROM @DemoEvents AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Events AS existing
    WHERE existing.Title = source.Title
);

SET @InsertedEvents = @@ROWCOUNT;

DECLARE @EventMvcWorkshopId int = (SELECT Id FROM dbo.Events WHERE Title = N'DEMO - MVC Live Coding Workshop');
DECLARE @EventPythonLabId int = (SELECT Id FROM dbo.Events WHERE Title = N'DEMO - Python Problem-Solving Lab');
DECLARE @EventAjaxClinicId int = (SELECT Id FROM dbo.Events WHERE Title = N'DEMO - AJAX Clinic and Q&A');
DECLARE @EventEfCoreSeminarId int = (SELECT Id FROM dbo.Events WHERE Title = N'DEMO - EF Core and SQL Server Seminar');
DECLARE @EventCancelledWebinarId int = (SELECT Id FROM dbo.Events WHERE Title = N'DEMO - Cancelled JavaScript Webinar');
DECLARE @EventDraftClinicId int = (SELECT Id FROM dbo.Events WHERE Title = N'DEMO - Draft Database Design Clinic');

IF @EventMvcWorkshopId IS NULL OR @EventPythonLabId IS NULL OR @EventAjaxClinicId IS NULL
    OR @EventEfCoreSeminarId IS NULL OR @EventCancelledWebinarId IS NULL OR @EventDraftClinicId IS NULL
BEGIN
    THROW 50005, 'Demo event seed failed while resolving event identifiers.', 1;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Announcements
    WHERE Title = N'DEMO - MVC Workshop Registration Open'
)
BEGIN
    INSERT INTO dbo.Announcements
    (
        CourseId, EventId, CreatedByUserId, Title, Content,
        Audience, Priority, Status, PublishedAt, ExpiresAt, CreatedAt, UpdatedAt
    )
    VALUES
    (
        @CourseMvc, @EventMvcWorkshopId, @AdminRef,
        N'DEMO - MVC Workshop Registration Open',
        N'Registration is open for the MVC Live Coding Workshop. Enrolled Students and Tutors can review the schedule and apply before the deadline.',
        N'All', N'Important', N'Published', DATEADD(day, -1, @NowUtc), DATEADD(day, 11, @NowUtc), DATEADD(day, -2, @NowUtc), @NowUtc
    );

    SET @InsertedAnnouncements += 1;
END
ELSE
BEGIN
    UPDATE dbo.Announcements
    SET CourseId = @CourseMvc,
        EventId = @EventMvcWorkshopId,
        CreatedByUserId = @AdminRef,
        Content = N'Registration is open for the MVC Live Coding Workshop. Enrolled Students and Tutors can review the schedule and apply before the deadline.',
        Audience = N'All',
        Priority = N'Important',
        Status = N'Published',
        PublishedAt = DATEADD(day, -1, @NowUtc),
        ExpiresAt = DATEADD(day, 11, @NowUtc),
        UpdatedAt = @NowUtc
    WHERE Title = N'DEMO - MVC Workshop Registration Open';

    SET @UpdatedAnnouncements += @@ROWCOUNT;
END;

DECLARE @DemoProposals TABLE
(
    SeedKey nvarchar(80) NOT NULL PRIMARY KEY,
    CourseId int NULL,
    Title nvarchar(150) NOT NULL,
    Description nvarchar(max) NOT NULL,
    Reason nvarchar(2000) NOT NULL,
    StartsAt datetimeoffset NULL,
    EndsAt datetimeoffset NULL,
    ApplicationDeadline datetimeoffset NULL,
    RegistrationAudience nvarchar(20) NOT NULL,
    Mode nvarchar(20) NULL,
    Location nvarchar(255) NULL,
    MeetingPlatform nvarchar(30) NULL,
    MeetingUrl nvarchar(2048) NULL,
    MaxParticipants int NULL,
    Status nvarchar(20) NOT NULL,
    ReviewedByUserId nvarchar(450) NULL,
    ReviewedAt datetimeoffset NULL,
    ReviewNote nvarchar(2000) NULL,
    LastRevisedAt datetimeoffset NULL,
    RevisionCount int NOT NULL,
    CreatedEventId int NULL,
    CreatedAt datetimeoffset NOT NULL
);

INSERT INTO @DemoProposals
(
    SeedKey, CourseId, Title, Description, Reason, StartsAt, EndsAt, ApplicationDeadline,
    RegistrationAudience, Mode, Location, MeetingPlatform, MeetingUrl, MaxParticipants,
    Status, ReviewedByUserId, ReviewedAt, ReviewNote, LastRevisedAt, RevisionCount, CreatedEventId, CreatedAt
)
VALUES
    (N'pending-stats', @CoursePython,
     N'DEMO - Statistics Revision Workshop Proposal',
     N'A Tutor-proposed revision workshop covering descriptive statistics, probability, and exam-style questions.',
     N'Students requested a focused revision session before the next assessment window.',
     DATEADD(day, 45, @NowUtc), DATEADD(hour, 2, DATEADD(day, 45, @NowUtc)), DATEADD(day, 42, @NowUtc),
     N'Student', N'Physical', N'Science Block Room 4', NULL, NULL, 30,
     N'Pending', NULL, NULL, NULL, NULL, 0, NULL, DATEADD(day, -2, @NowUtc)),
    (N'changes-uiux', @CourseEfCore,
     N'DEMO - UI/UX Portfolio Review Proposal',
     N'A Tutor-proposed portfolio review session for design coursework and presentation feedback.',
     N'The Tutor wants to give structured feedback before students submit their final design portfolios.',
     DATEADD(day, 38, @NowUtc), DATEADD(hour, 2, DATEADD(day, 38, @NowUtc)), DATEADD(day, 35, @NowUtc),
     N'Student', N'Online', NULL, N'GoogleMeet', N'https://meet.google.com/demo-uiux-review', 20,
     N'ChangesRequested', @AdminRef, DATEADD(day, -1, @NowUtc),
     N'Please add clearer learning outcomes and confirm the final participant limit before resubmission.',
     DATEADD(day, -1, @NowUtc), 1, NULL, DATEADD(day, -5, @NowUtc)),
    (N'rejected-overbooked', NULL,
     N'DEMO - Campus Open Day Proposal',
     N'A Tutor-proposed campus open day with multiple simultaneous activities and walk-in sessions.',
     N'This would introduce prospective Students to the platform and Tutor community.',
     DATEADD(day, 20, @NowUtc), DATEADD(hour, 6, DATEADD(day, 20, @NowUtc)), DATEADD(day, 17, @NowUtc),
     N'All', N'Physical', N'Main Campus Atrium', NULL, NULL, 500,
     N'Rejected', @AdminRef, DATEADD(day, -3, @NowUtc),
     N'The proposed capacity exceeds the available venue support for this semester.', NULL, 0, NULL, DATEADD(day, -6, @NowUtc)),
    (N'approved-db-clinic', @CourseEfCore,
     N'DEMO - Draft Database Design Clinic',
     N'A Tutor-proposed database design clinic linked to the EF Core and SQL Server course materials.',
     N'Students need help translating entity diagrams into maintainable EF Core models.',
     DATEADD(day, 42, @NowUtc), DATEADD(hour, 2, DATEADD(day, 42, @NowUtc)), DATEADD(day, 39, @NowUtc),
     N'Tutor', N'Online', NULL, N'Zoom', N'https://zoom.us/j/demo-db-clinic', 15,
     N'Approved', @AdminRef, DATEADD(day, -4, @NowUtc), N'Approved. Publish the draft event when the final agenda is ready.',
     NULL, 0, @EventDraftClinicId, DATEADD(day, -5, @NowUtc));

UPDATE existing
SET existing.CourseId = source.CourseId,
    existing.ProposedByUserId = @TutorRef,
    existing.Title = source.Title,
    existing.Description = source.Description,
    existing.Reason = source.Reason,
    existing.StartsAt = source.StartsAt,
    existing.EndsAt = source.EndsAt,
    existing.ApplicationDeadline = source.ApplicationDeadline,
    existing.RegistrationAudience = source.RegistrationAudience,
    existing.Mode = source.Mode,
    existing.Location = source.Location,
    existing.MeetingPlatform = source.MeetingPlatform,
    existing.MeetingUrl = source.MeetingUrl,
    existing.MaxParticipants = source.MaxParticipants,
    existing.Status = source.Status,
    existing.ReviewedByUserId = source.ReviewedByUserId,
    existing.ReviewedAt = source.ReviewedAt,
    existing.ReviewNote = source.ReviewNote,
    existing.LastRevisedAt = source.LastRevisedAt,
    existing.RevisionCount = source.RevisionCount,
    existing.CreatedEventId = source.CreatedEventId,
    existing.UpdatedAt = @NowUtc
FROM dbo.EventProposals AS existing
INNER JOIN @DemoProposals AS source ON source.Title = existing.Title;

SET @UpdatedProposals = @@ROWCOUNT;

INSERT INTO dbo.EventProposals
(
    CourseId, ProposedByUserId, Title, Description, Reason,
    StartsAt, EndsAt, ApplicationDeadline, RegistrationAudience, Mode,
    Location, MeetingPlatform, MeetingUrl, MaxParticipants, Status,
    ReviewedByUserId, ReviewedAt, ReviewNote, LastRevisedAt, RevisionCount,
    CreatedEventId, CreatedAt, UpdatedAt
)
SELECT
    source.CourseId,
    @TutorRef,
    source.Title,
    source.Description,
    source.Reason,
    source.StartsAt,
    source.EndsAt,
    source.ApplicationDeadline,
    source.RegistrationAudience,
    source.Mode,
    source.Location,
    source.MeetingPlatform,
    source.MeetingUrl,
    source.MaxParticipants,
    source.Status,
    source.ReviewedByUserId,
    source.ReviewedAt,
    source.ReviewNote,
    source.LastRevisedAt,
    source.RevisionCount,
    source.CreatedEventId,
    source.CreatedAt,
    @NowUtc
FROM @DemoProposals AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.EventProposals AS existing
    WHERE existing.Title = source.Title
);

SET @InsertedProposals = @@ROWCOUNT;

DECLARE @DemoRegistrations TABLE
(
    SeedKey nvarchar(80) NOT NULL PRIMARY KEY,
    EventId int NOT NULL,
    UserId int NOT NULL,
    Message nvarchar(1000) NULL,
    Status nvarchar(20) NOT NULL,
    ReviewedByUserId int NULL,
    ReviewedAt datetimeoffset NULL,
    ReviewNote nvarchar(1000) NULL,
    CreatedAt datetimeoffset NOT NULL
);

INSERT INTO @DemoRegistrations
    (SeedKey, EventId, UserId, Message, Status, ReviewedByUserId, ReviewedAt, ReviewNote, CreatedAt)
VALUES
    (N'mvc-approved', @EventMvcWorkshopId, @StudentId,
     N'I would like to join the live coding session and review controller patterns.',
     N'Approved', @TutorId, DATEADD(day, -1, @NowUtc), N'Approved. Please join five minutes early for setup.', DATEADD(day, -2, @NowUtc)),
    (N'python-pending', @EventPythonLabId, @StudentId,
     N'I want help practising Python loops and functions before the lab.',
     N'Pending', NULL, NULL, NULL, DATEADD(day, -1, @NowUtc)),
    (N'ajax-rejected', @EventAjaxClinicId, @StudentId,
     N'I need help debugging fetch requests in my project.',
     N'Rejected', @TutorId, DATEADD(day, -1, @NowUtc), N'The clinic is reserved for enrolled AJAX course Students this round.', DATEADD(day, -2, @NowUtc)),
    (N'cancelled-approved', @EventCancelledWebinarId, @StudentId,
     N'Please reserve a seat for the JavaScript webinar.',
     N'Approved', @TutorId, DATEADD(day, -4, @NowUtc), N'Approved before the event was cancelled.', DATEADD(day, -5, @NowUtc));

UPDATE existing
SET existing.Message = source.Message,
    existing.Status = source.Status,
    existing.ReviewedByUserId = source.ReviewedByUserId,
    existing.ReviewedAt = source.ReviewedAt,
    existing.ReviewNote = source.ReviewNote,
    existing.UpdatedAt = @NowUtc
FROM dbo.EventRegistrations AS existing
INNER JOIN @DemoRegistrations AS source
    ON source.EventId = existing.EventId
   AND source.UserId = existing.UserId;

SET @UpdatedRegistrations = @@ROWCOUNT;

INSERT INTO dbo.EventRegistrations
(
    EventId, UserId, Message, Status, ReviewedByUserId, ReviewedAt, ReviewNote, CreatedAt, UpdatedAt
)
SELECT
    source.EventId,
    source.UserId,
    source.Message,
    source.Status,
    source.ReviewedByUserId,
    source.ReviewedAt,
    source.ReviewNote,
    source.CreatedAt,
    @NowUtc
FROM @DemoRegistrations AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.EventRegistrations AS existing
    WHERE existing.EventId = source.EventId
      AND existing.UserId = source.UserId
);

SET @InsertedRegistrations = @@ROWCOUNT;

DECLARE @AnnWelcomeId int = (SELECT Id FROM dbo.Announcements WHERE Title = N'DEMO - Welcome to Online Tuition');
DECLARE @AnnMaintenanceId int = (SELECT Id FROM dbo.Announcements WHERE Title = N'DEMO - Scheduled Maintenance Notice');
DECLARE @AnnMvcCourseId int = (SELECT Id FROM dbo.Announcements WHERE Title = N'DEMO - MVC Assignment Brief Released');
DECLARE @AnnMvcEventId int = (SELECT Id FROM dbo.Announcements WHERE Title = N'DEMO - MVC Workshop Registration Open');
DECLARE @ProposalPendingId int = (SELECT Id FROM dbo.EventProposals WHERE Title = N'DEMO - Statistics Revision Workshop Proposal');
DECLARE @ProposalChangesId int = (SELECT Id FROM dbo.EventProposals WHERE Title = N'DEMO - UI/UX Portfolio Review Proposal');
DECLARE @ProposalRejectedId int = (SELECT Id FROM dbo.EventProposals WHERE Title = N'DEMO - Campus Open Day Proposal');
DECLARE @ProposalApprovedId int = (SELECT Id FROM dbo.EventProposals WHERE Title = N'DEMO - Draft Database Design Clinic');

DELETE FROM dbo.Notifications
WHERE Title LIKE N'DEMO - %'
   OR Title IN
   (
       N'DEMO - Welcome to Online Tuition',
       N'DEMO - Scheduled Maintenance Notice',
       N'DEMO - MVC Assignment Brief Released',
       N'DEMO - MVC Live Coding Workshop',
       N'DEMO - Python Problem-Solving Lab',
       N'DEMO - AJAX Clinic and Q&A',
       N'DEMO - EF Core and SQL Server Seminar',
       N'DEMO - Cancelled JavaScript Webinar',
       N'New event proposal: DEMO - Statistics Revision Workshop Proposal',
       N'Event proposal Approved: DEMO - Draft Database Design Clinic',
       N'Event proposal ChangesRequested: DEMO - UI/UX Portfolio Review Proposal',
       N'Event proposal Rejected: DEMO - Campus Open Day Proposal',
       N'Registration Approved: DEMO - MVC Live Coding Workshop',
       N'Registration Rejected: DEMO - AJAX Clinic and Q&A',
       N'Event cancelled: DEMO - Cancelled JavaScript Webinar'
   );

DECLARE @DemoNotifications TABLE
(
    UserId int NOT NULL,
    Type nvarchar(40) NOT NULL,
    Title nvarchar(250) NOT NULL,
    Message nvarchar(1000) NOT NULL,
    Details nvarchar(2000) NULL,
    TargetUrl nvarchar(500) NULL,
    ReadAt datetimeoffset NULL,
    CreatedAt datetimeoffset NOT NULL
);

INSERT INTO @DemoNotifications
    (UserId, Type, Title, Message, Details, TargetUrl, ReadAt, CreatedAt)
VALUES
    (@StudentId, N'AnnouncementPublished', N'DEMO - Welcome to Online Tuition',
     N'A new announcement has been published.', N'Priority: Normal', CONCAT(N'/Announcements/Details/', @AnnWelcomeId), DATEADD(day, -6, @NowUtc), DATEADD(day, -7, @NowUtc)),
    (@TutorId, N'AnnouncementPublished', N'DEMO - Welcome to Online Tuition',
     N'A new announcement has been published.', N'Priority: Normal', CONCAT(N'/Announcements/Details/', @AnnWelcomeId), NULL, DATEADD(day, -7, @NowUtc)),
    (@StudentId, N'AnnouncementPublished', N'DEMO - Scheduled Maintenance Notice',
     N'A new announcement has been published.', N'Priority: Urgent', CONCAT(N'/Announcements/Details/', @AnnMaintenanceId), NULL, DATEADD(day, -2, @NowUtc)),
    (@TutorId, N'AnnouncementPublished', N'DEMO - Scheduled Maintenance Notice',
     N'A new announcement has been published.', N'Priority: Urgent', CONCAT(N'/Announcements/Details/', @AnnMaintenanceId), NULL, DATEADD(day, -2, @NowUtc)),
    (@StudentId, N'AnnouncementPublished', N'DEMO - MVC Assignment Brief Released',
     N'A new announcement has been published.', N'Priority: Important', CONCAT(N'/Announcements/Details/', @AnnMvcCourseId), NULL, DATEADD(day, -1, @NowUtc)),
    (@StudentId, N'EventPublished', N'DEMO - MVC Live Coding Workshop',
     N'A new event has been published.', N'Online event on ' + CONVERT(nvarchar(30), DATEADD(day, 14, @NowUtc), 106), CONCAT(N'/Events/Details/', @EventMvcWorkshopId), NULL, DATEADD(day, -10, @NowUtc)),
    (@TutorId, N'EventPublished', N'DEMO - MVC Live Coding Workshop',
     N'A new event has been published.', N'Online event on ' + CONVERT(nvarchar(30), DATEADD(day, 14, @NowUtc), 106), CONCAT(N'/Events/Details/', @EventMvcWorkshopId), DATEADD(day, -9, @NowUtc), DATEADD(day, -10, @NowUtc)),
    (@StudentId, N'EventPublished', N'DEMO - Python Problem-Solving Lab',
     N'A new event has been published.', N'Physical event on ' + CONVERT(nvarchar(30), DATEADD(day, 21, @NowUtc), 106), CONCAT(N'/Events/Details/', @EventPythonLabId), NULL, DATEADD(day, -9, @NowUtc)),
    (@StudentId, N'EventPublished', N'DEMO - AJAX Clinic and Q&A',
     N'A new event has been published.', N'Online event on ' + CONVERT(nvarchar(30), DATEADD(day, 28, @NowUtc), 106), CONCAT(N'/Events/Details/', @EventAjaxClinicId), NULL, DATEADD(day, -8, @NowUtc)),
    (@TutorId, N'EventPublished', N'DEMO - EF Core and SQL Server Seminar',
     N'A new event has been published.', N'Hybrid event on ' + CONVERT(nvarchar(30), DATEADD(day, 35, @NowUtc), 106), CONCAT(N'/Events/Details/', @EventEfCoreSeminarId), NULL, DATEADD(day, -7, @NowUtc)),
    (@AdminId, N'ProposalSubmitted', N'New event proposal: DEMO - Statistics Revision Workshop Proposal',
     N'tutor@test.com submitted an event proposal for review.', N'Audience: Student; mode: Physical', CONCAT(N'/EventProposals/Details/', @ProposalPendingId), NULL, DATEADD(day, -2, @NowUtc)),
    (@TutorId, N'ProposalChangesRequested', N'Event proposal ChangesRequested: DEMO - UI/UX Portfolio Review Proposal',
     N'Admin requested changes to your event proposal.', N'Please add clearer learning outcomes and confirm the final participant limit before resubmission.', CONCAT(N'/EventProposals/Details/', @ProposalChangesId), NULL, DATEADD(day, -1, @NowUtc)),
    (@TutorId, N'ProposalRejected', N'Event proposal Rejected: DEMO - Campus Open Day Proposal',
     N'Your event proposal was rejected.', N'The proposed capacity exceeds the available venue support for this semester.', CONCAT(N'/EventProposals/Details/', @ProposalRejectedId), DATEADD(day, -2, @NowUtc), DATEADD(day, -3, @NowUtc)),
    (@TutorId, N'ProposalApproved', N'Event proposal Approved: DEMO - Draft Database Design Clinic',
     N'Your event proposal was approved.', N'Approved. Publish the draft event when the final agenda is ready.', CONCAT(N'/EventProposals/Details/', @ProposalApprovedId), NULL, DATEADD(day, -4, @NowUtc)),
    (@TutorId, N'RegistrationSubmitted', N'New registration: DEMO - MVC Live Coding Workshop',
     N'student@test.com submitted an event registration.', NULL, CONCAT(N'/EventRegistrations/Manage?eventId=', @EventMvcWorkshopId), DATEADD(day, -2, @NowUtc), DATEADD(day, -2, @NowUtc)),
    (@TutorId, N'RegistrationSubmitted', N'New registration: DEMO - Python Problem-Solving Lab',
     N'student@test.com submitted an event registration.', NULL, CONCAT(N'/EventRegistrations/Manage?eventId=', @EventPythonLabId), NULL, DATEADD(day, -1, @NowUtc)),
    (@StudentId, N'RegistrationApproved', N'Registration Approved: DEMO - MVC Live Coding Workshop',
     N'Your event registration was approved.', N'Approved. Please join five minutes early for setup.', N'/EventRegistrations/MyRegistrations', NULL, DATEADD(day, -1, @NowUtc)),
    (@StudentId, N'RegistrationRejected', N'Registration Rejected: DEMO - AJAX Clinic and Q&A',
     N'Your event registration was rejected.', N'The clinic is reserved for enrolled AJAX course Students this round.', N'/EventRegistrations/MyRegistrations', NULL, DATEADD(day, -1, @NowUtc)),
    (@StudentId, N'EventCancelled', N'Event cancelled: DEMO - Cancelled JavaScript Webinar',
     N'An event you registered for has been cancelled.', N'Reason: The tutor became unavailable and the session was rescheduled for a later semester.', N'/EventRegistrations/MyRegistrations', NULL, DATEADD(day, -1, @NowUtc)),
    (@TutorId, N'EventCancelled', N'Event cancelled: DEMO - Cancelled JavaScript Webinar',
     N'An event you organise has been cancelled.', N'Reason: The tutor became unavailable and the session was rescheduled for a later semester.', CONCAT(N'/Events/Details/', @EventCancelledWebinarId), NULL, DATEADD(day, -1, @NowUtc)),
    (@StudentId, N'AnnouncementPublished', N'DEMO - MVC Workshop Registration Open',
     N'A new announcement has been published.', N'Priority: Important', CONCAT(N'/Announcements/Details/', @AnnMvcEventId), NULL, DATEADD(day, -1, @NowUtc)),
    (@TutorId, N'AnnouncementPublished', N'DEMO - MVC Workshop Registration Open',
     N'A new announcement has been published.', N'Priority: Important', CONCAT(N'/Announcements/Details/', @AnnMvcEventId), NULL, DATEADD(day, -1, @NowUtc));

INSERT INTO dbo.Notifications
    (UserId, Type, Title, Message, Details, TargetUrl, ReadAt, CreatedAt)
SELECT
    source.UserId,
    source.Type,
    source.Title,
    source.Message,
    source.Details,
    source.TargetUrl,
    source.ReadAt,
    source.CreatedAt
FROM @DemoNotifications AS source;

SET @InsertedNotifications = @@ROWCOUNT;

COMMIT TRANSACTION;

SELECT
    @InsertedEnrollments AS InsertedEnrollments,
    @InsertedAnnouncements AS InsertedAnnouncements,
    @UpdatedAnnouncements AS UpdatedAnnouncements,
    @InsertedEvents AS InsertedEvents,
    @UpdatedEvents AS UpdatedEvents,
    @InsertedProposals AS InsertedEventProposals,
    @UpdatedProposals AS UpdatedEventProposals,
    @InsertedRegistrations AS InsertedEventRegistrations,
    @UpdatedRegistrations AS UpdatedEventRegistrations,
    @InsertedNotifications AS InsertedNotifications,
    (SELECT COUNT(*) FROM dbo.Announcements WHERE Title LIKE N'DEMO - %') AS DemoAnnouncementCount,
    (SELECT COUNT(*) FROM dbo.Events WHERE Title LIKE N'DEMO - %') AS DemoEventCount,
    (SELECT COUNT(*) FROM dbo.EventProposals WHERE Title LIKE N'DEMO - %') AS DemoProposalCount,
    (SELECT COUNT(*) FROM dbo.EventRegistrations AS registration INNER JOIN dbo.Events AS event ON event.Id = registration.EventId WHERE event.Title LIKE N'DEMO - %') AS DemoRegistrationCount,
    (SELECT COUNT(*) FROM dbo.Notifications WHERE Title LIKE N'DEMO - %' OR Title LIKE N'New event proposal: DEMO - %' OR Title LIKE N'Event proposal %: DEMO - %' OR Title LIKE N'Registration %: DEMO - %' OR Title LIKE N'Event cancelled: DEMO - %') AS DemoNotificationCount;

SELECT
    announcement.Id,
    announcement.Title,
    announcement.Audience,
    announcement.Priority,
    announcement.Status,
    course.Code AS CourseCode
FROM dbo.Announcements AS announcement
LEFT JOIN dbo.Courses AS course ON course.CourseId = announcement.CourseId
WHERE announcement.Title LIKE N'DEMO - %'
ORDER BY announcement.CreatedAt DESC;

SELECT
    event.Id,
    event.Title,
    event.Mode,
    event.Status,
    event.StartsAt,
    course.Code AS CourseCode
FROM dbo.Events AS event
LEFT JOIN dbo.Courses AS course ON course.CourseId = event.CourseId
WHERE event.Title LIKE N'DEMO - %'
ORDER BY event.StartsAt;

SELECT TOP (20)
    notification.Id,
    account.Email AS RecipientEmail,
    notification.Type,
    notification.Title,
    notification.ReadAt,
    notification.CreatedAt
FROM dbo.Notifications AS notification
INNER JOIN dbo.Users AS account ON account.Id = notification.UserId
WHERE notification.Title LIKE N'DEMO - %'
   OR notification.Title LIKE N'New event proposal: DEMO - %'
   OR notification.Title LIKE N'Event proposal %: DEMO - %'
   OR notification.Title LIKE N'Registration %: DEMO - %'
   OR notification.Title LIKE N'Event cancelled: DEMO - %'
ORDER BY notification.CreatedAt DESC;
GO
