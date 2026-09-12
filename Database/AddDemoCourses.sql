-- Adds 10 idempotent demonstration Course records only.
-- Prerequisites: at least one active Tutor, one Admin, and one active CourseCategory.
-- CourseStatus values: Draft = 1, PendingReview = 2, Published = 4.
USE [OnlineTuitionDb];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Tutor1Id int =
(
    SELECT MIN(Id)
    FROM dbo.Users
    WHERE Role = 1 AND IsBlocked = 0
);

DECLARE @Tutor2Id int =
(
    SELECT MIN(Id)
    FROM dbo.Users
    WHERE Role = 1 AND IsBlocked = 0 AND Id > @Tutor1Id
);

DECLARE @AdminId int =
(
    SELECT MIN(Id)
    FROM dbo.Users
    WHERE Role = 2 AND IsBlocked = 0
);

DECLARE @Category1Id int =
(
    SELECT MIN(CourseCategoryId)
    FROM dbo.CourseCategories
    WHERE IsActive = 1
);

DECLARE @Category2Id int =
(
    SELECT MIN(CourseCategoryId)
    FROM dbo.CourseCategories
    WHERE IsActive = 1 AND CourseCategoryId > @Category1Id
);

DECLARE @Category3Id int =
(
    SELECT MIN(CourseCategoryId)
    FROM dbo.CourseCategories
    WHERE IsActive = 1 AND CourseCategoryId > @Category2Id
);

IF @Tutor1Id IS NULL
BEGIN
    THROW 50001, 'Add at least one unblocked Tutor before running AddDemoCourses.sql.', 1;
END;

IF @AdminId IS NULL
BEGIN
    THROW 50002, 'Add at least one unblocked Admin before running AddDemoCourses.sql.', 1;
END;

IF @Category1Id IS NULL
BEGIN
    THROW 50003, 'Create at least one active Course Category before running AddDemoCourses.sql.', 1;
END;

-- Fall back to the first available record when fewer than two Tutors or
-- fewer than three active categories currently exist.
SET @Tutor2Id = COALESCE(@Tutor2Id, @Tutor1Id);
SET @Category2Id = COALESCE(@Category2Id, @Category1Id);
SET @Category3Id = COALESCE(@Category3Id, @Category2Id, @Category1Id);

DECLARE @NowUtc datetime2 = SYSUTCDATETIME();

DECLARE @DemoCourses TABLE
(
    SeedNumber int NOT NULL,
    Code nvarchar(30) NOT NULL,
    Title nvarchar(180) NOT NULL,
    Slug nvarchar(200) NOT NULL,
    ShortDescription nvarchar(500) NOT NULL,
    Description nvarchar(max) NOT NULL,
    Price decimal(10,2) NOT NULL,
    Status int NOT NULL
);

INSERT INTO @DemoCourses
    (SeedNumber, Code, Title, Slug, ShortDescription, Description, Price, Status)
VALUES
    (1, N'DEMO-CSHARP-101', N'C# Programming Essentials',
     N'demo-csharp-programming-essentials',
     N'Build a strong foundation in modern C# programming.',
     N'Learn variables, control flow, methods, collections, classes, and practical problem solving through guided examples and exercises.',
     0.00, 4),

    (2, N'DEMO-MVC-101', N'ASP.NET Core MVC Fundamentals',
     N'demo-aspnet-core-mvc-fundamentals',
     N'Create maintainable web applications using ASP.NET Core MVC.',
     N'Understand controllers, Razor views, ViewModels, validation, dependency injection, routing, and the request lifecycle by building a complete MVC application.',
     79.00, 4),

    (3, N'DEMO-EFCORE-201', N'Entity Framework Core and SQL Server',
     N'demo-entity-framework-core-sql-server',
     N'Model relational data and query SQL Server safely with EF Core.',
     N'Practice code-first entities, relationships, migrations, LINQ queries, validation, and safe create, update, and delete workflows.',
     99.00, 4),

    (4, N'DEMO-WEBUI-110', N'Razor and Bootstrap Web UI',
     N'demo-razor-bootstrap-web-ui',
     N'Design clear and responsive interfaces with Razor and Bootstrap.',
     N'Build layouts, partial views, responsive forms, tables, cards, navigation, validation feedback, and accessible page structures.',
     69.00, 4),

    (5, N'DEMO-MATH-F5', N'Form 5 Mathematics Mastery',
     N'demo-form-5-mathematics-mastery',
     N'Revise essential Form 5 mathematics concepts with worked examples.',
     N'Strengthen algebra, functions, geometry, statistics, and examination technique through structured lessons and progressive exercises.',
     59.00, 4),

    (6, N'DEMO-ENGLISH-120', N'Academic English Writing',
     N'demo-academic-english-writing',
     N'Write clearer academic paragraphs, essays, and reports.',
     N'Improve planning, paragraph structure, grammar, evidence use, editing, and formal academic style with practical writing activities.',
     49.00, 4),

    (7, N'DEMO-PHYSICS-210', N'Physics Problem Solving',
     N'demo-physics-problem-solving',
     N'Apply physics concepts confidently to structured problems.',
     N'Work through mechanics, electricity, waves, energy, calculations, diagrams, and examination-style problem-solving strategies.',
     89.00, 4),

    (8, N'DEMO-CHEM-210', N'Chemistry Exam Preparation',
     N'demo-chemistry-exam-preparation',
     N'Prepare for chemistry assessments with focused revision.',
     N'Review atomic structure, bonding, calculations, acids and bases, organic chemistry, experiments, and common examination questions.',
     89.00, 4),

    (9, N'DEMO-DSA-220', N'Data Structures with C#',
     N'demo-data-structures-with-csharp',
     N'Learn how common data structures support efficient programs.',
     N'Explore lists, stacks, queues, dictionaries, trees, searching, sorting, complexity, and implementation techniques using C#.',
     109.00, 2),

    (10, N'DEMO-WEB-START', N'Introduction to Web Development',
     N'demo-introduction-to-web-development',
     N'Start building web pages with a practical foundation.',
     N'Learn how browsers, HTTP, HTML, CSS, JavaScript, servers, and databases work together before progressing to full MVC applications.',
     39.00, 1);

BEGIN TRANSACTION;

INSERT INTO dbo.Courses
(
    TutorId,
    CourseCategoryId,
    ReviewedByUserId,
    Code,
    Title,
    Slug,
    ShortDescription,
    Description,
    ThumbnailPath,
    Price,
    Status,
    StatusBeforeSuspension,
    RejectionReason,
    SuspensionReason,
    ReviewedAtUtc,
    PublishedAtUtc,
    CreatedAtUtc,
    UpdatedAtUtc
)
SELECT
    CASE WHEN source.SeedNumber % 2 = 0 THEN @Tutor2Id ELSE @Tutor1Id END,
    CASE source.SeedNumber % 3
        WHEN 1 THEN @Category1Id
        WHEN 2 THEN @Category2Id
        ELSE @Category3Id
    END,
    CASE WHEN source.Status = 4 THEN @AdminId ELSE NULL END,
    source.Code,
    source.Title,
    source.Slug,
    source.ShortDescription,
    source.Description,
    NULL,
    source.Price,
    source.Status,
    NULL,
    NULL,
    NULL,
    CASE WHEN source.Status = 4 THEN DATEADD(day, -source.SeedNumber, @NowUtc) ELSE NULL END,
    CASE WHEN source.Status = 4 THEN DATEADD(day, -source.SeedNumber, @NowUtc) ELSE NULL END,
    DATEADD(day, -(source.SeedNumber + 10), @NowUtc),
    @NowUtc
FROM @DemoCourses AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Courses AS existing
    WHERE existing.Code = source.Code OR existing.Slug = source.Slug
);

DECLARE @InsertedCount int = @@ROWCOUNT;

COMMIT TRANSACTION;

SELECT @InsertedCount AS InsertedCourseCount;

SELECT
    CourseId,
    Code,
    Title,
    Price,
    Status,
    TutorId,
    CourseCategoryId
FROM dbo.Courses
WHERE Code LIKE N'DEMO-%'
ORDER BY CourseId;
GO
