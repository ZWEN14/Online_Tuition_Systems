-- Sample data for local demonstrations. Reruns skip existing sample titles.
-- Survey types: Text=1, Rating=2, MultipleChoice=3, YesNo=4, Checkbox=5, Dropdown=6, Upload=7.
USE [OnlineTuitionDb];
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @Admin int=(SELECT Id FROM Users WHERE Email=N'admin@test.com' AND Role=2);
DECLARE @Student int=(SELECT Id FROM Users WHERE Email=N'student@test.com' AND Role=0);
DECLARE @Tutor int=(SELECT Id FROM Users WHERE Email=N'tutor@test.com' AND Role=1);
IF @Admin IS NULL OR @Student IS NULL OR @Tutor IS NULL THROW 50001, 'Create the admin@test.com, student@test.com and tutor@test.com accounts first.', 1;
DECLARE @Survey int, @Question int, @Option int, @Complaint int, @Category int;
DECLARE @S1 int,@S2 int,@S3 int,@S4 int,@S5 int;
DECLARE @NewSurveys int=0,@NewComplaints int=0;

-- 01 - Weekly Learning Feedback
IF NOT EXISTS (SELECT 1 FROM Surveys WHERE Title=N'01 - Weekly Learning Feedback')
BEGIN
INSERT INTO Surveys (Title,Description,ExpiresAt,IsActive,CourseId,CreatorId,CreatedAt) VALUES (N'01 - Weekly Learning Feedback',N'Sequential sections: lesson review, learning needs, then final comments.',NULL,1,NULL,@Admin,SYSUTCDATETIME());
SET @Survey=CONVERT(int,SCOPE_IDENTITY());
SET @NewSurveys+=1;
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Lesson review',NULL,1,1,NULL);
SET @S1=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Learning needs',NULL,2,1,NULL);
SET @S2=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Final comments',NULL,3,1,NULL);
SET @S3=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S1,N'How useful was this week''s lesson?',2,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S2,N'Which topics would you like to practise?',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=3,NextSectionId=NULL WHERE Id=@S3;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S3,N'What should we improve next week?',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
END;

-- 02 - Choose a Support Topic
IF NOT EXISTS (SELECT 1 FROM Surveys WHERE Title=N'02 - Choose a Support Topic')
BEGIN
INSERT INTO Surveys (Title,Description,ExpiresAt,IsActive,CourseId,CreatorId,CreatedAt) VALUES (N'02 - Choose a Support Topic',N'The first answer routes to technical help or teaching feedback; both paths finish at the summary.',NULL,1,NULL,@Admin,SYSUTCDATETIME());
SET @Survey=CONVERT(int,SCOPE_IDENTITY());
SET @NewSurveys+=1;
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Support topic',NULL,1,1,NULL);
SET @S1=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Technical help',NULL,2,1,NULL);
SET @S2=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Teaching feedback',NULL,3,1,NULL);
SET @S3=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Summary',NULL,4,1,NULL);
SET @S4=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S1,N'What do you need help with?',3,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'Technical issue',1);
SET @Option=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveyBranchRules (QuestionOptionId,Action,DestinationSectionId) VALUES (@Option,2,@S2);
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'Teaching feedback',2);
SET @Option=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveyBranchRules (QuestionOptionId,Action,DestinationSectionId) VALUES (@Option,2,@S3);
UPDATE SurveySections SET AfterSectionAction=2,NextSectionId=@S4 WHERE Id=@S2;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S2,N'Describe the technical issue.',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=2,NextSectionId=@S4 WHERE Id=@S3;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S3,N'What could your tutor improve?',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=3,NextSectionId=NULL WHERE Id=@S4;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S4,N'How urgent is your request?',2,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
END;

-- 03 - Quick Satisfaction Check
IF NOT EXISTS (SELECT 1 FROM Surveys WHERE Title=N'03 - Quick Satisfaction Check')
BEGIN
INSERT INTO Surveys (Title,Description,ExpiresAt,IsActive,CourseId,CreatorId,CreatedAt) VALUES (N'03 - Quick Satisfaction Check',N'Satisfied respondents can submit immediately; others continue to detailed feedback.',NULL,1,NULL,@Admin,SYSUTCDATETIME());
SET @Survey=CONVERT(int,SCOPE_IDENTITY());
SET @NewSurveys+=1;
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Quick check',NULL,1,1,NULL);
SET @S1=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Tell us more',NULL,2,1,NULL);
SET @S2=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Follow-up',NULL,3,1,NULL);
SET @S3=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S1,N'Are you happy with your learning experience?',3,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'Yes, submit my feedback',1);
SET @Option=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveyBranchRules (QuestionOptionId,Action,DestinationSectionId) VALUES (@Option,3,NULL);
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'I would like to explain',2);
SET @Option=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveyBranchRules (QuestionOptionId,Action,DestinationSectionId) VALUES (@Option,2,@S2);
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S2,N'What could be better?',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=3,NextSectionId=NULL WHERE Id=@S3;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S3,N'Would you like a follow-up?',4,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
END;

-- 04 - Course Interest Paths
IF NOT EXISTS (SELECT 1 FROM Surveys WHERE Title=N'04 - Course Interest Paths')
BEGIN
INSERT INTO Surveys (Title,Description,ExpiresAt,IsActive,CourseId,CreatorId,CreatedAt) VALUES (N'04 - Course Interest Paths',N'A dropdown selects Mathematics, Programming, or Languages, then all paths join the final section.',NULL,1,NULL,@Admin,SYSUTCDATETIME());
SET @Survey=CONVERT(int,SCOPE_IDENTITY());
SET @NewSurveys+=1;
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Choose a subject',NULL,1,1,NULL);
SET @S1=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Mathematics',NULL,2,1,NULL);
SET @S2=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Programming',NULL,3,1,NULL);
SET @S3=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Languages',NULL,4,1,NULL);
SET @S4=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Availability',NULL,5,1,NULL);
SET @S5=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S1,N'Which subject interests you?',6,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'Mathematics',1);
SET @Option=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveyBranchRules (QuestionOptionId,Action,DestinationSectionId) VALUES (@Option,2,@S2);
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'Programming',2);
SET @Option=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveyBranchRules (QuestionOptionId,Action,DestinationSectionId) VALUES (@Option,2,@S3);
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'Languages',3);
SET @Option=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveyBranchRules (QuestionOptionId,Action,DestinationSectionId) VALUES (@Option,2,@S4);
UPDATE SurveySections SET AfterSectionAction=2,NextSectionId=@S5 WHERE Id=@S2;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S2,N'Which mathematics topics interest you?',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=2,NextSectionId=@S5 WHERE Id=@S3;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S3,N'Which programming skills would you like to learn?',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=2,NextSectionId=@S5 WHERE Id=@S4;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S4,N'Which language would you like to practise?',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=3,NextSectionId=NULL WHERE Id=@S5;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S5,N'When are you available for lessons?',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
END;

-- 05 - Section Skip Demonstration
IF NOT EXISTS (SELECT 1 FROM Surveys WHERE Title=N'05 - Section Skip Demonstration')
BEGIN
INSERT INTO Surveys (Title,Description,ExpiresAt,IsActive,CourseId,CreatorId,CreatedAt) VALUES (N'05 - Section Skip Demonstration',N'The first section goes directly to section 3. Section 2 is deliberately skipped.',NULL,1,NULL,@Admin,SYSUTCDATETIME());
SET @Survey=CONVERT(int,SCOPE_IDENTITY());
SET @NewSurveys+=1;
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Introduction',NULL,1,1,NULL);
SET @S1=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Skipped section',NULL,2,1,NULL);
SET @S2=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Goal details',NULL,3,1,NULL);
SET @S3=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Next steps',NULL,4,1,NULL);
SET @S4=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=2,NextSectionId=@S3 WHERE Id=@S1;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S1,N'What is your main learning goal?',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S2,N'This required question should be skipped.',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S3,N'How confident are you about reaching your goal?',2,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=3,NextSectionId=NULL WHERE Id=@S4;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S4,N'What support would help you?',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
END;

-- 06 - Early Section Submission
IF NOT EXISTS (SELECT 1 FROM Surveys WHERE Title=N'06 - Early Section Submission')
BEGIN
INSERT INTO Surveys (Title,Description,ExpiresAt,IsActive,CourseId,CreatorId,CreatedAt) VALUES (N'06 - Early Section Submission',N'After section 2, submit the form without visiting the later required section.',NULL,1,NULL,@Admin,SYSUTCDATETIME());
SET @Survey=CONVERT(int,SCOPE_IDENTITY());
SET @NewSurveys+=1;
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Lesson experience',NULL,1,1,NULL);
SET @S1=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Final feedback',NULL,2,1,NULL);
SET @S2=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Not on this path',NULL,3,1,NULL);
SET @S3=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S1,N'How would you rate the lesson?',2,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=3,NextSectionId=NULL WHERE Id=@S2;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S2,N'Share one useful takeaway.',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=3,NextSectionId=NULL WHERE Id=@S3;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S3,N'This question must not prevent early submission.',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
END;

-- 07 - Optional Evidence Route
IF NOT EXISTS (SELECT 1 FROM Surveys WHERE Title=N'07 - Optional Evidence Route')
BEGIN
INSERT INTO Surveys (Title,Description,ExpiresAt,IsActive,CourseId,CreatorId,CreatedAt) VALUES (N'07 - Optional Evidence Route',N'Choose to upload evidence or skip directly to final comments.',NULL,1,NULL,@Admin,SYSUTCDATETIME());
SET @Survey=CONVERT(int,SCOPE_IDENTITY());
SET @NewSurveys+=1;
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Evidence choice',NULL,1,1,NULL);
SET @S1=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Upload evidence',NULL,2,1,NULL);
SET @S2=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Final comments',NULL,3,1,NULL);
SET @S3=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S1,N'Would you like to attach supporting evidence?',3,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'Yes, upload files',1);
SET @Option=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveyBranchRules (QuestionOptionId,Action,DestinationSectionId) VALUES (@Option,2,@S2);
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'No, skip uploads',2);
SET @Option=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveyBranchRules (QuestionOptionId,Action,DestinationSectionId) VALUES (@Option,2,@S3);
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S2,N'Upload a screenshot or document.',7,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=3,NextSectionId=NULL WHERE Id=@S3;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S3,N'Describe your feedback.',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
END;

-- 08 - Learning Resources Checklist
IF NOT EXISTS (SELECT 1 FROM Surveys WHERE Title=N'08 - Learning Resources Checklist')
BEGIN
INSERT INTO Surveys (Title,Description,ExpiresAt,IsActive,CourseId,CreatorId,CreatedAt) VALUES (N'08 - Learning Resources Checklist',N'Multiple-select preferences followed by a rating and written feedback; checkbox answers do not branch.',NULL,1,NULL,@Admin,SYSUTCDATETIME());
SET @Survey=CONVERT(int,SCOPE_IDENTITY());
SET @NewSurveys+=1;
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Resources',NULL,1,1,NULL);
SET @S1=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Usefulness',NULL,2,1,NULL);
SET @S2=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Suggestions',NULL,3,1,NULL);
SET @S3=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S1,N'Which learning resources do you use?',5,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'Lesson recordings',1);
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'Practice worksheets',2);
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'Live sessions',3);
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'Discussion notes',4);
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S2,N'How useful are the available resources?',2,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=3,NextSectionId=NULL WHERE Id=@S3;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S3,N'What resources should we add?',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
END;

-- 09 - Section Default and Answer Override
IF NOT EXISTS (SELECT 1 FROM Surveys WHERE Title=N'09 - Section Default and Answer Override')
BEGIN
INSERT INTO Surveys (Title,Description,ExpiresAt,IsActive,CourseId,CreatorId,CreatedAt) VALUES (N'09 - Section Default and Answer Override',N'The section default skips to section 3; specific answers override it with section 2 or immediate submission.',NULL,1,NULL,@Admin,SYSUTCDATETIME());
SET @Survey=CONVERT(int,SCOPE_IDENTITY());
SET @NewSurveys+=1;
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Feedback route',NULL,1,1,NULL);
SET @S1=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Detailed feedback',NULL,2,1,NULL);
SET @S2=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Short feedback',NULL,3,1,NULL);
SET @S3=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Finish',NULL,4,1,NULL);
SET @S4=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=2,NextSectionId=@S3 WHERE Id=@S1;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S1,N'How would you like to proceed?',3,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'Use the section default: short feedback',1);
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'Give detailed feedback',2);
SET @Option=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveyBranchRules (QuestionOptionId,Action,DestinationSectionId) VALUES (@Option,2,@S2);
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'Finish now',3);
SET @Option=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveyBranchRules (QuestionOptionId,Action,DestinationSectionId) VALUES (@Option,3,NULL);
UPDATE SurveySections SET AfterSectionAction=2,NextSectionId=@S4 WHERE Id=@S2;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S2,N'Describe your experience in detail.',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=2,NextSectionId=@S4 WHERE Id=@S3;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S3,N'Give an overall rating.',2,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=3,NextSectionId=NULL WHERE Id=@S4;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S4,N'Any final suggestions?',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
END;

-- 10 - Two-Stage Follow-Up
IF NOT EXISTS (SELECT 1 FROM Surveys WHERE Title=N'10 - Two-Stage Follow-Up')
BEGIN
INSERT INTO Surveys (Title,Description,ExpiresAt,IsActive,CourseId,CreatorId,CreatedAt) VALUES (N'10 - Two-Stage Follow-Up',N'First choose whether to continue; a later dropdown selects scheduling or learning-material feedback.',NULL,1,NULL,@Admin,SYSUTCDATETIME());
SET @Survey=CONVERT(int,SCOPE_IDENTITY());
SET @NewSurveys+=1;
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Participation',NULL,1,1,NULL);
SET @S1=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Choose a topic',NULL,2,1,NULL);
SET @S2=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Scheduling',NULL,3,1,NULL);
SET @S3=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Learning materials',NULL,4,1,NULL);
SET @S4=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveySections (SurveyId,Title,Description,DisplayOrder,AfterSectionAction,NextSectionId) VALUES (@Survey,N'Final rating',NULL,5,1,NULL);
SET @S5=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S1,N'Would you like to provide feedback?',3,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'Yes, continue',1);
SET @Option=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveyBranchRules (QuestionOptionId,Action,DestinationSectionId) VALUES (@Option,2,@S2);
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'No, submit now',2);
SET @Option=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveyBranchRules (QuestionOptionId,Action,DestinationSectionId) VALUES (@Option,3,NULL);
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S2,N'What is your feedback about?',6,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'Scheduling',1);
SET @Option=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveyBranchRules (QuestionOptionId,Action,DestinationSectionId) VALUES (@Option,2,@S3);
INSERT INTO QuestionOptions (QuestionId,Text,DisplayOrder) VALUES (@Question,N'Learning materials',2);
SET @Option=CONVERT(int,SCOPE_IDENTITY());
INSERT INTO SurveyBranchRules (QuestionOptionId,Action,DestinationSectionId) VALUES (@Option,2,@S4);
UPDATE SurveySections SET AfterSectionAction=2,NextSectionId=@S5 WHERE Id=@S3;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S3,N'What scheduling changes would help?',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=2,NextSectionId=@S5 WHERE Id=@S4;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S4,N'What materials would you like to improve?',1,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
UPDATE SurveySections SET AfterSectionAction=3,NextSectionId=NULL WHERE Id=@S5;
INSERT INTO Questions (SurveyId,SectionId,Text,Type,IsRequired,DisplayOrder) VALUES (@Survey,@S5,N'Rate your overall experience.',2,1,1);
SET @Question=CONVERT(int,SCOPE_IDENTITY());
END;

-- 01 - Lesson video will not load
IF NOT EXISTS (SELECT 1 FROM Complaints WHERE Title=N'01 - Lesson video will not load')
BEGIN
SET @Category=(SELECT Id FROM ComplaintCategories WHERE Name=N'Technical Issue');
IF @Category IS NULL THROW 50002, 'Required complaint category is missing.', 1;
INSERT INTO Complaints (Title,Description,CategoryId,UserId,AssignedTutorId,Status,ResolutionNotes,CreatedAt,UpdatedAt) VALUES (N'01 - Lesson video will not load',N'The recording for the latest lesson stops at the loading screen on my laptop.',@Category,@Student,NULL,1,NULL,DATEADD(day,-10,SYSUTCDATETIME()),DATEADD(day,-9,SYSUTCDATETIME()));
SET @Complaint=CONVERT(int,SCOPE_IDENTITY());
SET @NewComplaints+=1;
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,1,N'Sample complaint submitted.',@Student,DATEADD(day,-10,SYSUTCDATETIME()));
END;

-- 02 - Worksheet answers appear incorrect
IF NOT EXISTS (SELECT 1 FROM Complaints WHERE Title=N'02 - Worksheet answers appear incorrect')
BEGIN
SET @Category=(SELECT Id FROM ComplaintCategories WHERE Name=N'Course Content');
IF @Category IS NULL THROW 50002, 'Required complaint category is missing.', 1;
INSERT INTO Complaints (Title,Description,CategoryId,UserId,AssignedTutorId,Status,ResolutionNotes,CreatedAt,UpdatedAt) VALUES (N'02 - Worksheet answers appear incorrect',N'Questions 4 and 7 in the practice worksheet appear inconsistent with the worked examples.',@Category,@Student,@Tutor,2,N'Tutor is reviewing the worksheet answers.',DATEADD(day,-9,SYSUTCDATETIME()),DATEADD(day,-8,SYSUTCDATETIME()));
SET @Complaint=CONVERT(int,SCOPE_IDENTITY());
SET @NewComplaints+=1;
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,1,N'Sample complaint submitted.',@Student,DATEADD(day,-9,SYSUTCDATETIME()));
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,2,N'Tutor is reviewing the worksheet answers.',@Tutor,DATEADD(day,-8,SYSUTCDATETIME()));
END;

-- 03 - Audio was unclear during class
IF NOT EXISTS (SELECT 1 FROM Complaints WHERE Title=N'03 - Audio was unclear during class')
BEGIN
SET @Category=(SELECT Id FROM ComplaintCategories WHERE Name=N'Tutor / Teaching');
IF @Category IS NULL THROW 50002, 'Required complaint category is missing.', 1;
INSERT INTO Complaints (Title,Description,CategoryId,UserId,AssignedTutorId,Status,ResolutionNotes,CreatedAt,UpdatedAt) VALUES (N'03 - Audio was unclear during class',N'The microphone volume was too low to follow the explanations during the evening lesson.',@Category,@Student,@Tutor,3,N'Tutor adjusted the microphone and supplied a clear recording.',DATEADD(day,-8,SYSUTCDATETIME()),DATEADD(day,-7,SYSUTCDATETIME()));
SET @Complaint=CONVERT(int,SCOPE_IDENTITY());
SET @NewComplaints+=1;
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,1,N'Sample complaint submitted.',@Student,DATEADD(day,-8,SYSUTCDATETIME()));
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,2,N'Investigation started.',@Tutor,DATEADD(hour,1,DATEADD(day,-8,SYSUTCDATETIME())));
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,3,N'Tutor adjusted the microphone and supplied a clear recording.',@Tutor,DATEADD(day,-7,SYSUTCDATETIME()));
END;

-- 04 - Payment charged twice
IF NOT EXISTS (SELECT 1 FROM Complaints WHERE Title=N'04 - Payment charged twice')
BEGIN
SET @Category=(SELECT Id FROM ComplaintCategories WHERE Name=N'Billing / Payment');
IF @Category IS NULL THROW 50002, 'Required complaint category is missing.', 1;
INSERT INTO Complaints (Title,Description,CategoryId,UserId,AssignedTutorId,Status,ResolutionNotes,CreatedAt,UpdatedAt) VALUES (N'04 - Payment charged twice',N'Two charges are shown for the same monthly lesson package. Please review the payment records.',@Category,@Student,NULL,2,N'Admin is checking the payment references.',DATEADD(day,-7,SYSUTCDATETIME()),DATEADD(day,-6,SYSUTCDATETIME()));
SET @Complaint=CONVERT(int,SCOPE_IDENTITY());
SET @NewComplaints+=1;
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,1,N'Sample complaint submitted.',@Student,DATEADD(day,-7,SYSUTCDATETIME()));
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,2,N'Admin is checking the payment references.',@Admin,DATEADD(day,-6,SYSUTCDATETIME()));
END;

-- 05 - Request outside the support scope
IF NOT EXISTS (SELECT 1 FROM Complaints WHERE Title=N'05 - Request outside the support scope')
BEGIN
SET @Category=(SELECT Id FROM ComplaintCategories WHERE Name=N'Other');
IF @Category IS NULL THROW 50002, 'Required complaint category is missing.', 1;
INSERT INTO Complaints (Title,Description,CategoryId,UserId,AssignedTutorId,Status,ResolutionNotes,CreatedAt,UpdatedAt) VALUES (N'05 - Request outside the support scope',N'Please arrange transport to a private event unrelated to the tuition service.',@Category,@Student,NULL,4,N'Request is outside the tuition service support scope.',DATEADD(day,-6,SYSUTCDATETIME()),DATEADD(day,-5,SYSUTCDATETIME()));
SET @Complaint=CONVERT(int,SCOPE_IDENTITY());
SET @NewComplaints+=1;
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,1,N'Sample complaint submitted.',@Student,DATEADD(day,-6,SYSUTCDATETIME()));
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,4,N'Request is outside the tuition service support scope.',@Admin,DATEADD(day,-5,SYSUTCDATETIME()));
END;

-- 06 - Unable to upload teaching material
IF NOT EXISTS (SELECT 1 FROM Complaints WHERE Title=N'06 - Unable to upload teaching material')
BEGIN
SET @Category=(SELECT Id FROM ComplaintCategories WHERE Name=N'Technical Issue');
IF @Category IS NULL THROW 50002, 'Required complaint category is missing.', 1;
INSERT INTO Complaints (Title,Description,CategoryId,UserId,AssignedTutorId,Status,ResolutionNotes,CreatedAt,UpdatedAt) VALUES (N'06 - Unable to upload teaching material',N'My lesson document upload fails before completion, even after retrying.',@Category,@Tutor,NULL,1,NULL,DATEADD(day,-5,SYSUTCDATETIME()),DATEADD(day,-4,SYSUTCDATETIME()));
SET @Complaint=CONVERT(int,SCOPE_IDENTITY());
SET @NewComplaints+=1;
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,1,N'Sample complaint submitted.',@Tutor,DATEADD(day,-5,SYSUTCDATETIME()));
END;

-- 07 - Lesson content needs more examples
IF NOT EXISTS (SELECT 1 FROM Complaints WHERE Title=N'07 - Lesson content needs more examples')
BEGIN
SET @Category=(SELECT Id FROM ComplaintCategories WHERE Name=N'Course Content');
IF @Category IS NULL THROW 50002, 'Required complaint category is missing.', 1;
INSERT INTO Complaints (Title,Description,CategoryId,UserId,AssignedTutorId,Status,ResolutionNotes,CreatedAt,UpdatedAt) VALUES (N'07 - Lesson content needs more examples',N'The introduction to algebra would be easier to follow with additional worked examples.',@Category,@Student,NULL,1,NULL,DATEADD(day,-4,SYSUTCDATETIME()),DATEADD(day,-3,SYSUTCDATETIME()));
SET @Complaint=CONVERT(int,SCOPE_IDENTITY());
SET @NewComplaints+=1;
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,1,N'Sample complaint submitted.',@Student,DATEADD(day,-4,SYSUTCDATETIME()));
END;

-- 08 - Tutor arrived late
IF NOT EXISTS (SELECT 1 FROM Complaints WHERE Title=N'08 - Tutor arrived late')
BEGIN
SET @Category=(SELECT Id FROM ComplaintCategories WHERE Name=N'Tutor / Teaching');
IF @Category IS NULL THROW 50002, 'Required complaint category is missing.', 1;
INSERT INTO Complaints (Title,Description,CategoryId,UserId,AssignedTutorId,Status,ResolutionNotes,CreatedAt,UpdatedAt) VALUES (N'08 - Tutor arrived late',N'The tutor joined the lesson fifteen minutes after the scheduled start time.',@Category,@Student,@Tutor,3,N'Tutor apologised and arranged a replacement session.',DATEADD(day,-3,SYSUTCDATETIME()),DATEADD(day,-2,SYSUTCDATETIME()));
SET @Complaint=CONVERT(int,SCOPE_IDENTITY());
SET @NewComplaints+=1;
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,1,N'Sample complaint submitted.',@Student,DATEADD(day,-3,SYSUTCDATETIME()));
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,2,N'Investigation started.',@Tutor,DATEADD(hour,1,DATEADD(day,-3,SYSUTCDATETIME())));
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,3,N'Tutor apologised and arranged a replacement session.',@Tutor,DATEADD(day,-2,SYSUTCDATETIME()));
END;

-- 09 - Invoice description is unclear
IF NOT EXISTS (SELECT 1 FROM Complaints WHERE Title=N'09 - Invoice description is unclear')
BEGIN
SET @Category=(SELECT Id FROM ComplaintCategories WHERE Name=N'Billing / Payment');
IF @Category IS NULL THROW 50002, 'Required complaint category is missing.', 1;
INSERT INTO Complaints (Title,Description,CategoryId,UserId,AssignedTutorId,Status,ResolutionNotes,CreatedAt,UpdatedAt) VALUES (N'09 - Invoice description is unclear',N'The invoice does not explain which lesson dates are covered by the fee.',@Category,@Student,NULL,3,N'Admin supplied an itemised invoice with lesson dates.',DATEADD(day,-2,SYSUTCDATETIME()),DATEADD(day,-1,SYSUTCDATETIME()));
SET @Complaint=CONVERT(int,SCOPE_IDENTITY());
SET @NewComplaints+=1;
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,1,N'Sample complaint submitted.',@Student,DATEADD(day,-2,SYSUTCDATETIME()));
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,2,N'Investigation started.',@Admin,DATEADD(hour,1,DATEADD(day,-2,SYSUTCDATETIME())));
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,3,N'Admin supplied an itemised invoice with lesson dates.',@Admin,DATEADD(day,-1,SYSUTCDATETIME()));
END;

-- 10 - Student request notification delayed
IF NOT EXISTS (SELECT 1 FROM Complaints WHERE Title=N'10 - Student request notification delayed')
BEGIN
SET @Category=(SELECT Id FROM ComplaintCategories WHERE Name=N'Other');
IF @Category IS NULL THROW 50002, 'Required complaint category is missing.', 1;
INSERT INTO Complaints (Title,Description,CategoryId,UserId,AssignedTutorId,Status,ResolutionNotes,CreatedAt,UpdatedAt) VALUES (N'10 - Student request notification delayed',N'A student booking request appeared in my notifications much later than expected.',@Category,@Tutor,NULL,2,N'Admin is reviewing notification delivery times.',DATEADD(day,-1,SYSUTCDATETIME()),DATEADD(day,-0,SYSUTCDATETIME()));
SET @Complaint=CONVERT(int,SCOPE_IDENTITY());
SET @NewComplaints+=1;
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,1,N'Sample complaint submitted.',@Tutor,DATEADD(day,-1,SYSUTCDATETIME()));
INSERT INTO ComplaintStatusHistories (ComplaintId,Status,Note,UpdatedByUserId,ChangedAt) VALUES (@Complaint,2,N'Admin is reviewing notification delivery times.',@Admin,DATEADD(day,-0,SYSUTCDATETIME()));
END;
COMMIT TRANSACTION;
SELECT @NewSurveys AS InsertedSurveys,@NewComplaints AS InsertedComplaints;
