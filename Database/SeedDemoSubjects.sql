-- Development/demo data for Subject Overview.
-- Run against the OnlineTuitionDb database. Existing subjects are not changed.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

INSERT INTO dbo.Subjects ([Name], [Description], [BaseCost])
SELECT demo.[Name], demo.[Description], demo.[BaseCost]
FROM (VALUES
    (N'Mathematics', N'Build confidence in algebra, geometry and problem solving.', CAST(40.00 AS decimal(10, 2))),
    (N'English Language', N'Improve reading, writing, grammar and communication skills.', CAST(38.00 AS decimal(10, 2))),
    (N'Physics', N'Explore motion, energy, electricity and scientific reasoning.', CAST(50.00 AS decimal(10, 2))),
    (N'Chemistry', N'Learn chemical reactions, the periodic table and laboratory concepts.', CAST(50.00 AS decimal(10, 2))),
    (N'Biology', N'Study living systems, human biology and the natural world.', CAST(45.00 AS decimal(10, 2))),
    (N'Computer Science', N'Practise programming fundamentals and computational thinking.', CAST(55.00 AS decimal(10, 2)))
) AS demo ([Name], [Description], [BaseCost])
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Subjects AS existing
    WHERE existing.[Name] = demo.[Name]
);

-- Use only the named development Tutor, if present; do not change other tutors.
DECLARE @demoTutorId int = (
    SELECT TOP (1) u.Id
    FROM dbo.Users AS u
    WHERE u.[Name] = N'Tutor' AND u.[Role] = 1
    ORDER BY u.Id
);

IF @demoTutorId IS NOT NULL
BEGIN
    INSERT INTO dbo.TutorSubjects (TutorId, SubjectId, CreatedAt)
    SELECT @demoTutorId, s.Id, GETDATE()
    FROM dbo.Subjects AS s
    WHERE s.[Name] IN (N'Mathematics', N'Physics', N'Computer Science')
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.TutorSubjects AS existing
          WHERE existing.TutorId = @demoTutorId
            AND existing.SubjectId = s.Id
      );
END;

COMMIT TRANSACTION;

SELECT s.Id, s.[Name], s.BaseCost, COUNT(ts.Id) AS TutorCount
FROM dbo.Subjects AS s
LEFT JOIN dbo.TutorSubjects AS ts ON ts.SubjectId = s.Id
GROUP BY s.Id, s.[Name], s.BaseCost
ORDER BY s.Id;
