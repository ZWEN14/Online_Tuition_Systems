USE [OnlineTuitionDb];
GO

-- Show the application tables in this database.
SELECT
    TABLE_SCHEMA,
    TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
GO

-- All registered accounts. PasswordHash is intentionally not selected.
SELECT
    Id,
    Email,
    Role,
    CreatedAt
FROM dbo.Users
ORDER BY CreatedAt DESC;
GO

-- Event proposals.
SELECT TOP (100)
    Id,
    ProposedByUserId,
    Title,
    Mode,
    Status,
    ReviewedByUserId,
    ReviewedAt,
    CreatedEventId,
    CreatedAt
FROM dbo.EventProposals
ORDER BY CreatedAt DESC;
GO

-- Events created from approved proposals.
SELECT TOP (100)
    Id,
    Title,
    StartsAt,
    EndsAt,
    RegistrationAudience,
    Mode,
    MaxParticipants,
    Status,
    CreatedAt
FROM dbo.Events
ORDER BY CreatedAt DESC;
GO

-- Event registrations, including the applicant and event title.
SELECT TOP (100)
    registration.Id,
    registration.EventId,
    event.Title AS EventTitle,
    registration.UserId,
    account.Email AS ApplicantEmail,
    account.Role AS ApplicantRole,
    registration.Status,
    registration.ReviewNote,
    registration.ReviewedByUserId,
    registration.ReviewedAt,
    registration.CreatedAt
FROM dbo.EventRegistrations AS registration
INNER JOIN dbo.Events AS event ON event.Id = registration.EventId
INNER JOIN dbo.Users AS account ON account.Id = registration.UserId
ORDER BY registration.CreatedAt DESC;
GO

-- Announcements.
SELECT TOP (100)
    Id,
    EventId,
    Title,
    Audience,
    Priority,
    Status,
    PublishedAt,
    ExpiresAt,
    CreatedAt
FROM dbo.Announcements
ORDER BY CreatedAt DESC;
GO

-- Quick row counts for each module table.
SELECT 'Users' AS TableName, COUNT(*) AS [RowCount] FROM dbo.Users
UNION ALL
SELECT 'EventProposals', COUNT(*) FROM dbo.EventProposals
UNION ALL
SELECT 'Events', COUNT(*) FROM dbo.Events
UNION ALL
SELECT 'EventRegistrations', COUNT(*) FROM dbo.EventRegistrations
UNION ALL
SELECT 'Announcements', COUNT(*) FROM dbo.Announcements;
GO
