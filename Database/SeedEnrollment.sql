-- Enrolls the 15 named demo Students in Advanced C# Development.
-- Safe to rerun: existing matching Enrollments are refreshed, not duplicated.
-- This is roster/access demonstration data only; it does not create Payments or Invoices.
USE [OnlineTuitionDb];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @NowUtc datetime2 = SYSUTCDATETIME();
DECLARE @CourseId int;
DECLARE @CourseStatus int;

DECLARE @DemoStudents TABLE
(
    SeedOrder int NOT NULL PRIMARY KEY,
    Email nvarchar(100) NOT NULL UNIQUE
);

INSERT INTO @DemoStudents (SeedOrder, Email)
VALUES
    (1, N'olivia.carter@test.com'),
    (2, N'liam.bennett@test.com'),
    (3, N'emma.collins@test.com'),
    (4, N'noah.parker@test.com'),
    (5, N'ava.mitchell@test.com'),
    (6, N'ethan.walker@test.com'),
    (7, N'sophia.turner@test.com'),
    (8, N'mason.harris@test.com'),
    (9, N'mia.cooper@test.com'),
    (10, N'lucas.morgan@test.com'),
    (11, N'isabella.reed@test.com'),
    (12, N'james.foster@test.com'),
    (13, N'charlotte.hayes@test.com'),
    (14, N'benjamin.ward@test.com'),
    (15, N'amelia.brooks@test.com');

SELECT
    @CourseId = CourseId,
    @CourseStatus = Status
FROM dbo.Courses
WHERE Code = N'DEMO-CSHARP-ADV'
    AND Title = N'Advanced C# Development';

IF @CourseId IS NULL
BEGIN
    THROW 51000,
        'Advanced C# Development (DEMO-CSHARP-ADV) does not exist. Run AddDemoCourses.sql first.',
        1;
END;

-- CourseStatus 4 is Published.
IF @CourseStatus <> 4
BEGIN
    THROW 51001,
        'Advanced C# Development exists but is not Published.',
        1;
END;

IF
(
    SELECT COUNT(*)
    FROM @DemoStudents AS demo
    INNER JOIN dbo.Users AS studentUser
        ON studentUser.Email = demo.Email
        AND studentUser.Role = 0
        AND studentUser.EmailVerified = 1
        AND studentUser.IsBlocked = 0
) <> 15
BEGIN
    THROW 51002,
        'One or more of the 15 expected active Student accounts is missing. Run AddDemoUsers.sql first.',
        1;
END;

BEGIN TRANSACTION;

UPDATE existing
SET existing.Status = 2,
    existing.EnrolledAtUtc = DATEADD(day, -demo.SeedOrder, @NowUtc),
    existing.ActivatedAtUtc = DATEADD(day, -demo.SeedOrder, @NowUtc),
    existing.CancelledAtUtc = NULL
FROM dbo.Enrollments AS existing
INNER JOIN dbo.Users AS studentUser ON studentUser.Id = existing.StudentId
INNER JOIN @DemoStudents AS demo ON demo.Email = studentUser.Email
WHERE existing.CourseId = @CourseId;

DECLARE @UpdatedCount int = @@ROWCOUNT;

INSERT INTO dbo.Enrollments
    (StudentId, CourseId, Status, EnrolledAtUtc, ActivatedAtUtc, CancelledAtUtc)
SELECT
    studentUser.Id,
    @CourseId,
    2,
    DATEADD(day, -demo.SeedOrder, @NowUtc),
    DATEADD(day, -demo.SeedOrder, @NowUtc),
    NULL
FROM @DemoStudents AS demo
INNER JOIN dbo.Users AS studentUser ON studentUser.Email = demo.Email
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Enrollments AS existing
    WHERE existing.StudentId = studentUser.Id
        AND existing.CourseId = @CourseId
);

DECLARE @InsertedCount int = @@ROWCOUNT;

COMMIT TRANSACTION;

SELECT
    @CourseId AS CourseId,
    N'Advanced C# Development' AS CourseTitle,
    @InsertedCount AS InsertedEnrollmentCount,
    @UpdatedCount AS UpdatedEnrollmentCount,
    COUNT(*) AS ActiveDemoStudentCount
FROM dbo.Enrollments AS enrollment
INNER JOIN dbo.Users AS studentUser ON studentUser.Id = enrollment.StudentId
INNER JOIN @DemoStudents AS demo ON demo.Email = studentUser.Email
WHERE enrollment.CourseId = @CourseId
    AND enrollment.Status = 2;

SELECT
    studentUser.Name,
    studentUser.Email,
    enrollment.EnrolledAtUtc,
    enrollment.ActivatedAtUtc
FROM dbo.Enrollments AS enrollment
INNER JOIN dbo.Users AS studentUser ON studentUser.Id = enrollment.StudentId
INNER JOIN @DemoStudents AS demo ON demo.Email = studentUser.Email
WHERE enrollment.CourseId = @CourseId
    AND enrollment.Status = 2
ORDER BY enrollment.EnrolledAtUtc DESC;
GO
