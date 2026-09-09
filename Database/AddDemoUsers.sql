-- Run this file from Visual Studio:
-- SQL Server Object Explorer > OnlineTuitionDb > New Query.
-- All three demo accounts use this password: qwer1234!
-- Role values: Student = 0, Tutor = 1, Admin = 2.

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Remove the former demo accounts, if they are still present.
DELETE FROM dbo.Users
WHERE Email IN (
    N'admin@ots.com',
    N'tutor@ots.com',
    N'student@ots.com'
);

DECLARE @AdminId int;
DECLARE @TutorId int;
DECLARE @StudentId int;

-- This is an ASP.NET Core PasswordHasher hash for qwer1234!.
DECLARE @AdminHash nvarchar(100) =
    N'AQAAAAIAAYagAAAAEOyB8x+5Ve9d/VxJ5TjDkdj/uqDFg95mnJZDhy2Dm4KzNmybMxewTItg1pdQQRaTvQ==';
DECLARE @TutorHash nvarchar(100) =
    N'AQAAAAIAAYagAAAAEFzKa680f6tN4jODz7j30ap4TH/erSMng9MfbnXHrI1cnu1fgb31JlUzWPrhOGtOtw==';
DECLARE @StudentHash nvarchar(100) =
    N'AQAAAAIAAYagAAAAEPHtmRlQfGPgymAGExFXvkYWWQY8rKij8HA2sgaegeD3h8MNJZrxaxxE8VAXswcVCA==';

SELECT @AdminId = Id
FROM dbo.Users
WHERE Email = N'leowzw-wm23@student.tarc.edu.my';

IF @AdminId IS NULL
BEGIN
    INSERT INTO dbo.Users
        (Name, Email, Hash, EmailVerified, FailedLoginAttempts, IsBlocked, Role, CreatedAt)
    VALUES
        (N'Admin', N'leowzw-wm23@student.tarc.edu.my', @AdminHash, 1, 0, 0, 2, SYSUTCDATETIME());

    SET @AdminId = CONVERT(int, SCOPE_IDENTITY());
END
ELSE
BEGIN
    UPDATE dbo.Users
    SET Name = N'Admin',
        Hash = @AdminHash,
        EmailVerified = 1,
        FailedLoginAttempts = 0,
        LockoutEnd = NULL,
        IsBlocked = 0,
        Role = 2
    WHERE Id = @AdminId;
END;

SELECT @TutorId = Id
FROM dbo.Users
WHERE Email = N'lzwen05@gmail.com';

IF @TutorId IS NULL
BEGIN
    INSERT INTO dbo.Users
        (Name, Email, Hash, EmailVerified, FailedLoginAttempts, IsBlocked, Role, CreatedAt)
    VALUES
        (N'Tutor', N'lzwen05@gmail.com', @TutorHash, 1, 0, 0, 1, SYSUTCDATETIME());

    SET @TutorId = CONVERT(int, SCOPE_IDENTITY());
END
ELSE
BEGIN
    UPDATE dbo.Users
    SET Name = N'Tutor',
        Hash = @TutorHash,
        EmailVerified = 1,
        FailedLoginAttempts = 0,
        LockoutEnd = NULL,
        IsBlocked = 0,
        Role = 1
    WHERE Id = @TutorId;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Tutors WHERE UserId = @TutorId)
BEGIN
    INSERT INTO dbo.Tutors (Rating, UserId)
    VALUES (0, @TutorId);
END;

SELECT @StudentId = Id
FROM dbo.Users
WHERE Email = N'lzwen74@gmail.com';

IF @StudentId IS NULL
BEGIN
    INSERT INTO dbo.Users
        (Name, Email, Hash, EmailVerified, FailedLoginAttempts, IsBlocked, Role, CreatedAt)
    VALUES
        (N'Student', N'lzwen74@gmail.com', @StudentHash, 1, 0, 0, 0, SYSUTCDATETIME());

    SET @StudentId = CONVERT(int, SCOPE_IDENTITY());
END
ELSE
BEGIN
    UPDATE dbo.Users
    SET Name = N'Student',
        Hash = @StudentHash,
        EmailVerified = 1,
        FailedLoginAttempts = 0,
        LockoutEnd = NULL,
        IsBlocked = 0,
        Role = 0
    WHERE Id = @StudentId;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE UserId = @StudentId)
BEGIN
    INSERT INTO dbo.Students (EducationLevel, UserId)
    VALUES (N'Undergraduate', @StudentId);
END;

COMMIT TRANSACTION;

SELECT
    Id,
    Name,
    Email,
    CASE Role
        WHEN 0 THEN N'Student'
        WHEN 1 THEN N'Tutor'
        WHEN 2 THEN N'Admin'
    END AS Role,
    EmailVerified,
    IsBlocked
FROM dbo.Users
WHERE Email IN (
    N'leowzw-wm23@student.tarc.edu.my',
    N'lzwen05@gmail.com',
    N'lzwen74@gmail.com'
)
ORDER BY Role DESC;
