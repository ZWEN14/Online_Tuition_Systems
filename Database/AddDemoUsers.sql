-- Creates or refreshes three generic development accounts.
-- All accounts use the password: password123
-- Role values: Student = 0, Tutor = 1, Admin = 2.
USE [OnlineTuitionDb];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @StudentId int;
DECLARE @TutorId int;
DECLARE @AdminId int;

-- ASP.NET Core PasswordHasher hashes for password123.
DECLARE @StudentHash nvarchar(100) =
    N'AQAAAAIAAYagAAAAEGvMete6XfP5yiReKnt81JPF45s8paPoZXHqnSLVooRb949af4CIppRIsx66Epjl4g==';
DECLARE @TutorHash nvarchar(100) =
    N'AQAAAAIAAYagAAAAEMtWYRqFYuDLI7xGPLdFLfVQNAERfW+rXjeKVWZXHCMT+m4Pdyb72w0eYHOMolJWVw==';
DECLARE @AdminHash nvarchar(100) =
    N'AQAAAAIAAYagAAAAEHEInd+zPmxrP22JYsqxZpkj0DiC0CwespxNNqlD6w4hRh9VwOegFiC8qOsPiTlKwg==';

SELECT @StudentId = Id FROM dbo.Users WHERE Email = N'student@test.com';

IF @StudentId IS NULL
BEGIN
    INSERT INTO dbo.Users
        (Name, Email, Hash, EmailVerified, FailedLoginAttempts, IsBlocked, Role, CreatedAt)
    VALUES
        (N'Student', N'student@test.com', @StudentHash, 1, 0, 0, 0, SYSUTCDATETIME());

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

SELECT @TutorId = Id FROM dbo.Users WHERE Email = N'tutor@test.com';

IF @TutorId IS NULL
BEGIN
    INSERT INTO dbo.Users
        (Name, Email, Hash, EmailVerified, FailedLoginAttempts, IsBlocked, Role, CreatedAt)
    VALUES
        (N'Tutor', N'tutor@test.com', @TutorHash, 1, 0, 0, 1, SYSUTCDATETIME());

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

SELECT @AdminId = Id FROM dbo.Users WHERE Email = N'admin@test.com';

IF @AdminId IS NULL
BEGIN
    INSERT INTO dbo.Users
        (Name, Email, Hash, EmailVerified, FailedLoginAttempts, IsBlocked, Role, CreatedAt)
    VALUES
        (N'Administrator', N'admin@test.com', @AdminHash, 1, 0, 0, 2, SYSUTCDATETIME());

    SET @AdminId = CONVERT(int, SCOPE_IDENTITY());
END
ELSE
BEGIN
    UPDATE dbo.Users
    SET Name = N'Administrator',
        Hash = @AdminHash,
        EmailVerified = 1,
        FailedLoginAttempts = 0,
        LockoutEnd = NULL,
        IsBlocked = 0,
        Role = 2
    WHERE Id = @AdminId;
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
WHERE Email IN (N'student@test.com', N'tutor@test.com', N'admin@test.com')
ORDER BY Role DESC;
GO
