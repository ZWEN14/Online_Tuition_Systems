-- Creates or refreshes three generic development accounts and 15 named Students.
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

-- Fixed names keep this script reproducible while providing realistic varied data.
DECLARE @DemoStudents TABLE
(
    Name nvarchar(100) NOT NULL,
    Email nvarchar(100) NOT NULL PRIMARY KEY,
    EducationLevel nvarchar(50) NOT NULL
);

INSERT INTO @DemoStudents (Name, Email, EducationLevel)
VALUES
    (N'Olivia Carter', N'olivia.carter@test.com', N'Undergraduate'),
    (N'Liam Bennett', N'liam.bennett@test.com', N'Undergraduate'),
    (N'Emma Collins', N'emma.collins@test.com', N'Foundation'),
    (N'Noah Parker', N'noah.parker@test.com', N'Undergraduate'),
    (N'Ava Mitchell', N'ava.mitchell@test.com', N'Diploma'),
    (N'Ethan Walker', N'ethan.walker@test.com', N'Undergraduate'),
    (N'Sophia Turner', N'sophia.turner@test.com', N'Foundation'),
    (N'Mason Harris', N'mason.harris@test.com', N'Diploma'),
    (N'Mia Cooper', N'mia.cooper@test.com', N'Undergraduate'),
    (N'Lucas Morgan', N'lucas.morgan@test.com', N'Undergraduate'),
    (N'Isabella Reed', N'isabella.reed@test.com', N'Diploma'),
    (N'James Foster', N'james.foster@test.com', N'Foundation'),
    (N'Charlotte Hayes', N'charlotte.hayes@test.com', N'Undergraduate'),
    (N'Benjamin Ward', N'benjamin.ward@test.com', N'Diploma'),
    (N'Amelia Brooks', N'amelia.brooks@test.com', N'Undergraduate');

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

UPDATE existing
SET existing.Name = demo.Name,
    existing.Hash = @StudentHash,
    existing.EmailVerified = 1,
    existing.FailedLoginAttempts = 0,
    existing.LockoutEnd = NULL,
    existing.IsBlocked = 0,
    existing.Role = 0
FROM dbo.Users AS existing
INNER JOIN @DemoStudents AS demo ON demo.Email = existing.Email;

INSERT INTO dbo.Users
    (Name, Email, Hash, EmailVerified, FailedLoginAttempts, IsBlocked, Role, CreatedAt)
SELECT
    demo.Name,
    demo.Email,
    @StudentHash,
    1,
    0,
    0,
    0,
    SYSUTCDATETIME()
FROM @DemoStudents AS demo
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Users AS existing
    WHERE existing.Email = demo.Email
);

UPDATE studentProfile
SET studentProfile.EducationLevel = demo.EducationLevel
FROM dbo.Students AS studentProfile
INNER JOIN dbo.Users AS studentUser ON studentUser.Id = studentProfile.UserId
INNER JOIN @DemoStudents AS demo ON demo.Email = studentUser.Email;

INSERT INTO dbo.Students (EducationLevel, UserId)
SELECT demo.EducationLevel, studentUser.Id
FROM @DemoStudents AS demo
INNER JOIN dbo.Users AS studentUser ON studentUser.Email = demo.Email
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Students AS existingProfile
    WHERE existingProfile.UserId = studentUser.Id
);

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
    OR Email IN (SELECT Email FROM @DemoStudents)
ORDER BY Role DESC;
GO
