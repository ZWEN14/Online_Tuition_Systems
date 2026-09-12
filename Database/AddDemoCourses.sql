-- Creates 11 Course categories and 30 varied demonstration Courses.
-- Safe to rerun: named categories are reused and DEMO courses are refreshed.
-- Prerequisite: run Database/AddDemoUsers.sql first.
-- CourseStatus values: Draft = 1, PendingReview = 2, Published = 4.
USE [OnlineTuitionDb];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @TutorId int =
(
    SELECT TOP (1) Id
    FROM dbo.Users
    WHERE Email = N'tutor@test.com' AND Role = 1 AND IsBlocked = 0
);

DECLARE @AdminId int =
(
    SELECT TOP (1) Id
    FROM dbo.Users
    WHERE Email = N'admin@test.com' AND Role = 2 AND IsBlocked = 0
);

IF @TutorId IS NULL
BEGIN
    THROW 50001, 'Run Database/AddDemoUsers.sql to create tutor@test.com first.', 1;
END;

IF @AdminId IS NULL
BEGIN
    THROW 50002, 'Run Database/AddDemoUsers.sql to create admin@test.com first.', 1;
END;

DECLARE @NowUtc datetime2 = SYSUTCDATETIME();

DECLARE @DemoCategories TABLE
(
    Name nvarchar(80) NOT NULL,
    Description nvarchar(300) NOT NULL
);

INSERT INTO @DemoCategories (Name, Description)
VALUES
    (N'Programming & Software', N'Programming languages, software engineering, and application development.'),
    (N'Web Development', N'Frontend, backend, and full-stack web application development.'),
    (N'Data & AI', N'Databases, data analysis, machine learning, and artificial intelligence.'),
    (N'Mathematics', N'Core mathematics, advanced mathematics, statistics, and problem solving.'),
    (N'Science', N'Physics, chemistry, biology, and general scientific learning.'),
    (N'English & Languages', N'Academic writing, communication, and language examination preparation.'),
    (N'Business & Accounting', N'Accounting, entrepreneurship, marketing, and business fundamentals.'),
    (N'Design & Creative', N'User experience, visual communication, and creative digital skills.'),
    (N'Exam Preparation', N'Focused revision strategies and preparation for major assessments.'),
    (N'Personal Development', N'Communication, productivity, and practical personal skills.'),
    (N'Other', N'Useful learning topics that do not fit another Course category.');

BEGIN TRANSACTION;

INSERT INTO dbo.CourseCategories
    (Name, Description, IsActive, CreatedAtUtc, UpdatedAtUtc)
SELECT
    source.Name,
    source.Description,
    1,
    @NowUtc,
    @NowUtc
FROM @DemoCategories AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.CourseCategories AS existing
    WHERE existing.Name = source.Name
);

UPDATE existing
SET existing.Description = source.Description,
    existing.IsActive = 1,
    existing.UpdatedAtUtc = @NowUtc
FROM dbo.CourseCategories AS existing
INNER JOIN @DemoCategories AS source ON source.Name = existing.Name;

DECLARE @DemoCourses TABLE
(
    SeedNumber int NOT NULL,
    CategoryName nvarchar(80) NOT NULL,
    Code nvarchar(30) NOT NULL,
    Title nvarchar(180) NOT NULL,
    Slug nvarchar(200) NOT NULL,
    ShortDescription nvarchar(500) NOT NULL,
    Description nvarchar(max) NOT NULL,
    Price decimal(10,2) NOT NULL,
    Status int NOT NULL
);

INSERT INTO @DemoCourses
    (SeedNumber, CategoryName, Code, Title, Slug, ShortDescription, Description, Price, Status)
VALUES
    (1, N'Programming & Software', N'DEMO-CSHARP-101', N'C# Programming Essentials', N'demo-csharp-programming-essentials',
     N'Build a strong foundation in modern C# programming.', N'Learn variables, control flow, methods, collections, classes, and practical problem solving through guided exercises.', 0.00, 4),
    (2, N'Programming & Software', N'DEMO-CSHARP-ADV', N'Advanced C# Development', N'demo-advanced-csharp-development',
     N'Write cleaner and more capable C# applications.', N'Explore generics, delegates, asynchronous programming, LINQ, exception handling, and maintainable application design.', 129.00, 4),
    (3, N'Programming & Software', N'DEMO-PYTHON-101', N'Python for Beginners', N'demo-python-for-beginners',
     N'Start programming with practical Python lessons.', N'Learn Python syntax, decisions, loops, functions, collections, files, and small problem-solving projects.', 59.00, 4),

    (4, N'Web Development', N'DEMO-MVC-101', N'ASP.NET Core MVC Fundamentals', N'demo-aspnet-core-mvc-fundamentals',
     N'Create maintainable web applications using ASP.NET Core MVC.', N'Understand controllers, Razor views, ViewModels, validation, routing, dependency injection, and MVC request handling.', 89.00, 4),
    (5, N'Web Development', N'DEMO-WEBUI-110', N'Razor and Bootstrap Web UI', N'demo-razor-bootstrap-web-ui',
     N'Design clear and responsive interfaces with Razor and Bootstrap.', N'Build layouts, partial views, forms, tables, cards, navigation, validation feedback, and responsive pages.', 69.00, 4),
    (6, N'Web Development', N'DEMO-AJAX-120', N'JavaScript and AJAX Essentials', N'demo-javascript-ajax-essentials',
     N'Create responsive interactions without full page reloads.', N'Use JavaScript, DOM events, fetch requests, JSON, server endpoints, error handling, and progressive enhancement.', 79.00, 4),

    (7, N'Data & AI', N'DEMO-EFCORE-201', N'Entity Framework Core and SQL Server', N'demo-entity-framework-core-sql-server',
     N'Model relational data and query SQL Server safely with EF Core.', N'Practice code-first entities, relationships, migrations, LINQ, validation, and safe database operations.', 109.00, 4),
    (8, N'Data & AI', N'DEMO-SQL-150', N'SQL Data Analysis', N'demo-sql-data-analysis',
     N'Turn relational data into useful information.', N'Write SELECT queries, joins, grouping, subqueries, common table expressions, and practical reports.', 89.00, 4),
    (9, N'Data & AI', N'DEMO-ML-INTRO', N'Introduction to Machine Learning', N'demo-introduction-machine-learning',
     N'Understand the foundations of predictive models.', N'Explore datasets, features, training, evaluation, classification, regression, and responsible use of models.', 139.00, 4),

    (10, N'Mathematics', N'DEMO-MATH-F5', N'Form 5 Mathematics Mastery', N'demo-form-5-mathematics-mastery',
     N'Revise essential Form 5 mathematics with worked examples.', N'Strengthen algebra, functions, geometry, statistics, and examination technique with progressive exercises.', 59.00, 4),
    (11, N'Mathematics', N'DEMO-CALCULUS-101', N'Calculus Foundations', N'demo-calculus-foundations',
     N'Understand limits, differentiation, and integration.', N'Build conceptual understanding and solve guided calculus problems involving functions, rates, and areas.', 79.00, 4),
    (12, N'Mathematics', N'DEMO-STATS-101', N'Practical Statistics', N'demo-practical-statistics',
     N'Interpret data using essential statistical methods.', N'Learn descriptive statistics, probability, distributions, sampling, correlation, and clear interpretation.', 75.00, 4),

    (13, N'Science', N'DEMO-PHYSICS-210', N'Physics Problem Solving', N'demo-physics-problem-solving',
     N'Apply physics concepts confidently to structured problems.', N'Work through mechanics, electricity, waves, energy, calculations, diagrams, and examination strategies.', 89.00, 4),
    (14, N'Science', N'DEMO-CHEM-210', N'Chemistry Exam Preparation', N'demo-chemistry-exam-preparation',
     N'Prepare for chemistry assessments with focused revision.', N'Review atomic structure, bonding, calculations, acids, organic chemistry, experiments, and exam questions.', 89.00, 4),
    (15, N'Science', N'DEMO-BIO-205', N'Biology Concepts and Applications', N'demo-biology-concepts-applications',
     N'Connect biological systems to real-world examples.', N'Study cells, genetics, human systems, ecology, experimental skills, and structured biological explanations.', 85.00, 4),

    (16, N'English & Languages', N'DEMO-ENG-WRITE', N'Academic English Writing', N'demo-academic-english-writing',
     N'Write clearer academic paragraphs, essays, and reports.', N'Improve planning, structure, grammar, evidence use, editing, and formal academic style.', 49.00, 4),
    (17, N'English & Languages', N'DEMO-ENG-SPEAK', N'Confident English Speaking', N'demo-confident-english-speaking',
     N'Build fluency and confidence in everyday speaking.', N'Practise pronunciation, useful vocabulary, conversation structure, presentations, and active listening.', 45.00, 4),
    (18, N'English & Languages', N'DEMO-IELTS-101', N'IELTS Preparation Essentials', N'demo-ielts-preparation-essentials',
     N'Prepare systematically for all four IELTS components.', N'Practise listening, reading, writing, speaking, time management, and common assessment tasks.', 99.00, 4),

    (19, N'Business & Accounting', N'DEMO-ACC-101', N'Accounting Fundamentals', N'demo-accounting-fundamentals',
     N'Understand the basic language of business finance.', N'Learn transactions, journals, ledgers, financial statements, adjustments, and simple financial analysis.', 79.00, 4),
    (20, N'Business & Accounting', N'DEMO-MKT-120', N'Digital Marketing Basics', N'demo-digital-marketing-basics',
     N'Plan practical online marketing activities.', N'Explore customer needs, content, search, social media, campaign goals, measurement, and ethical marketing.', 69.00, 4),
    (21, N'Business & Accounting', N'DEMO-BIZ-START', N'Entrepreneurship Starter', N'demo-entrepreneurship-starter',
     N'Turn a simple idea into a practical business plan.', N'Validate problems, understand customers, design value propositions, estimate costs, and present an idea.', 65.00, 4),

    (22, N'Design & Creative', N'DEMO-UIUX-101', N'UI and UX Design Foundations', N'demo-ui-ux-design-foundations',
     N'Design interfaces that are clear and easy to use.', N'Learn user needs, information hierarchy, wireframes, visual consistency, usability, and accessibility.', 89.00, 4),
    (23, N'Design & Creative', N'DEMO-GRAPHIC-101', N'Graphic Design Essentials', N'demo-graphic-design-essentials',
     N'Create balanced and purposeful visual designs.', N'Practise typography, colour, composition, branding, image selection, and constructive design critique.', 59.00, 4),
    (24, N'Design & Creative', N'DEMO-CANVA-START', N'Canva for Learning Materials', N'demo-canva-learning-materials',
     N'Create polished educational graphics efficiently.', N'Design presentations, worksheets, social graphics, and reusable visual templates for learning.', 39.00, 4),

    (25, N'Exam Preparation', N'DEMO-SPM-STUDY', N'SPM Study Strategy', N'demo-spm-study-strategy',
     N'Build a realistic and effective SPM revision plan.', N'Use topic planning, active recall, practice questions, review cycles, and examination time management.', 35.00, 4),
    (26, N'Exam Preparation', N'DEMO-MUET-101', N'MUET Preparation Workshop', N'demo-muet-preparation-workshop',
     N'Prepare for MUET tasks with guided practice.', N'Develop reading, listening, speaking, and writing strategies aligned with common MUET tasks.', 69.00, 4),
    (27, N'Exam Preparation', N'DEMO-EXAM-TIME', N'Exam Time Management', N'demo-exam-time-management',
     N'Use limited examination time more effectively.', N'Practise question selection, time allocation, answer planning, checking, and managing examination pressure.', 29.00, 4),

    (28, N'Personal Development', N'DEMO-COMM-101', N'Effective Communication Skills', N'demo-effective-communication-skills',
     N'Communicate ideas clearly in study and work.', N'Improve listening, message structure, feedback, teamwork, presentation confidence, and professional communication.', 49.00, 4),
    (29, N'Personal Development', N'DEMO-PRODUCTIVITY', N'Personal Productivity Systems', N'demo-personal-productivity-systems',
     N'Build practical habits for focused learning.', N'Plan priorities, manage tasks, reduce distractions, review progress, and create sustainable routines.', 39.00, 2),
    (30, N'Other', N'DEMO-DIGITAL-LIFE', N'Digital Citizenship and Online Safety', N'demo-digital-citizenship-online-safety',
     N'Use online services safely and responsibly.', N'Learn privacy, account security, digital footprints, respectful participation, misinformation awareness, and reporting.', 0.00, 1);

UPDATE existing
SET existing.CourseCategoryId = category.CourseCategoryId,
    existing.TutorId = @TutorId,
    existing.ReviewedByUserId = CASE WHEN source.Status = 4 THEN @AdminId ELSE NULL END,
    existing.Title = source.Title,
    existing.Slug = source.Slug,
    existing.ShortDescription = source.ShortDescription,
    existing.Description = source.Description,
    existing.Price = source.Price,
    existing.Status = source.Status,
    existing.StatusBeforeSuspension = NULL,
    existing.RejectionReason = NULL,
    existing.SuspensionReason = NULL,
    existing.ReviewedAtUtc = CASE WHEN source.Status = 4 THEN DATEADD(day, -source.SeedNumber, @NowUtc) ELSE NULL END,
    existing.PublishedAtUtc = CASE WHEN source.Status = 4 THEN DATEADD(day, -source.SeedNumber, @NowUtc) ELSE NULL END,
    existing.UpdatedAtUtc = @NowUtc
FROM dbo.Courses AS existing
INNER JOIN @DemoCourses AS source ON source.Code = existing.Code
INNER JOIN dbo.CourseCategories AS category ON category.Name = source.CategoryName;

DECLARE @UpdatedCount int = @@ROWCOUNT;

INSERT INTO dbo.Courses
(
    TutorId, CourseCategoryId, ReviewedByUserId, Code, Title, Slug,
    ShortDescription, Description, ThumbnailPath, Price, Status,
    StatusBeforeSuspension, RejectionReason, SuspensionReason,
    ReviewedAtUtc, PublishedAtUtc, CreatedAtUtc, UpdatedAtUtc
)
SELECT
    @TutorId,
    category.CourseCategoryId,
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
    DATEADD(day, -(source.SeedNumber + 30), @NowUtc),
    @NowUtc
FROM @DemoCourses AS source
INNER JOIN dbo.CourseCategories AS category ON category.Name = source.CategoryName
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Courses AS existing
    WHERE existing.Code = source.Code
);

DECLARE @InsertedCount int = @@ROWCOUNT;

COMMIT TRANSACTION;

SELECT
    @InsertedCount AS InsertedCourseCount,
    @UpdatedCount AS UpdatedCourseCount,
    (SELECT COUNT(*) FROM dbo.CourseCategories WHERE Name IN (SELECT Name FROM @DemoCategories)) AS DemoCategoryCount,
    (SELECT COUNT(*) FROM dbo.Courses WHERE Code IN (SELECT Code FROM @DemoCourses)) AS DemoCourseCount;

SELECT
    course.CourseId,
    course.Code,
    course.Title,
    category.Name AS Category,
    course.Price,
    course.Status
FROM dbo.Courses AS course
INNER JOIN dbo.CourseCategories AS category
    ON category.CourseCategoryId = course.CourseCategoryId
WHERE course.Code IN (SELECT Code FROM @DemoCourses)
ORDER BY category.Name, course.Title;
GO
