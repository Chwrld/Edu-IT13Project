/*
    EduCRM Master Initializer
    - Drops and recreates the EduCRM database
    - Builds all schema objects (tables, constraints, indexes)
    - Seeds deterministic system accounts plus rich demo data
    - Generates 10 teachers × 3 classes × 30 students with sample conversations
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_ID('EduCRM') IS NOT NULL
BEGIN
    ALTER DATABASE [EduCRM] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [EduCRM];
END
GO

CREATE DATABASE [EduCRM];
GO

USE [EduCRM];
GO

------------------------------------------------------------
-- Core Tables
------------------------------------------------------------

CREATE TABLE dbo.users (
    user_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    email NVARCHAR(255) NOT NULL UNIQUE,
    password_hash NVARCHAR(MAX) NOT NULL,
    password_salt NVARCHAR(MAX) NOT NULL,
    role NVARCHAR(50) NOT NULL,
    display_name NVARCHAR(255) NOT NULL,
    phone_number NVARCHAR(20),
    address NVARCHAR(500),
    status NVARCHAR(20) NOT NULL DEFAULT 'active',
    archive_reason NVARCHAR(255),
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    created_by UNIQUEIDENTIFIER NULL,
    updated_at DATETIME2 NULL,
    updated_by UNIQUEIDENTIFIER NULL,
    FOREIGN KEY (created_by) REFERENCES dbo.users(user_id),
    FOREIGN KEY (updated_by) REFERENCES dbo.users(user_id)
);
GO

CREATE INDEX idx_users_email ON dbo.users(email);
GO

CREATE TABLE dbo.advisers (
    adviser_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    department NVARCHAR(255) NOT NULL,
    office_location NVARCHAR(255),
    consultation_hours NVARCHAR(255),
    FOREIGN KEY (adviser_id) REFERENCES dbo.users(user_id)
);
GO

CREATE TABLE dbo.admins (
    admin_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    FOREIGN KEY (admin_id) REFERENCES dbo.users(user_id)
);
GO

CREATE TABLE dbo.courses (
    course_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    course_code NVARCHAR(50) NOT NULL UNIQUE,
    course_name NVARCHAR(255) NOT NULL,
    credits INT NOT NULL DEFAULT 3,
    schedule NVARCHAR(255),
    professor_name NVARCHAR(255),
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    created_by UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY (created_by) REFERENCES dbo.users(user_id)
);
GO

CREATE INDEX idx_courses_professor ON dbo.courses(created_by);
GO

CREATE TABLE dbo.students (
    student_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    student_number NVARCHAR(50) NOT NULL UNIQUE,
    program NVARCHAR(255),
    year_level NVARCHAR(50),
    gpa DECIMAL(3,2),
    status NVARCHAR(50) NOT NULL DEFAULT 'active',
    adviser_id UNIQUEIDENTIFIER,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (student_id) REFERENCES dbo.users(user_id),
    FOREIGN KEY (adviser_id) REFERENCES dbo.users(user_id)
);
GO

CREATE INDEX idx_students_number ON dbo.students(student_number);
CREATE INDEX idx_students_adviser ON dbo.students(adviser_id);
GO

CREATE TABLE dbo.student_courses (
    enrollment_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    student_id UNIQUEIDENTIFIER NOT NULL,
    course_id UNIQUEIDENTIFIER NOT NULL,
    teacher_id UNIQUEIDENTIFIER NOT NULL,
    enrolled_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (student_id) REFERENCES dbo.students(student_id),
    FOREIGN KEY (course_id) REFERENCES dbo.courses(course_id),
    FOREIGN KEY (teacher_id) REFERENCES dbo.users(user_id)
);
GO

CREATE INDEX idx_student_courses_student ON dbo.student_courses(student_id);
CREATE INDEX idx_student_courses_course ON dbo.student_courses(course_id);
GO

CREATE TABLE dbo.class_assignments (
    assignment_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    course_id UNIQUEIDENTIFIER NOT NULL,
    title NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX) NOT NULL,
    deadline DATETIME2 NOT NULL,
    total_points INT NOT NULL DEFAULT 100,
    created_by UNIQUEIDENTIFIER NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (course_id) REFERENCES dbo.courses(course_id),
    FOREIGN KEY (created_by) REFERENCES dbo.users(user_id)
);
GO

CREATE INDEX idx_class_assignments_course ON dbo.class_assignments(course_id);
GO

CREATE TABLE dbo.assignment_submissions (
    submission_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    assignment_id UNIQUEIDENTIFIER NOT NULL,
    student_id UNIQUEIDENTIFIER NOT NULL,
    submitted_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    score INT NULL,
    status NVARCHAR(20) NOT NULL DEFAULT 'submitted',
    notes NVARCHAR(MAX),
    submission_content NVARCHAR(MAX),
    FOREIGN KEY (assignment_id) REFERENCES dbo.class_assignments(assignment_id),
    FOREIGN KEY (student_id) REFERENCES dbo.students(student_id),
    CONSTRAINT uq_assignment_student UNIQUE (assignment_id, student_id)
);
GO

CREATE INDEX idx_assignment_submissions_assignment ON dbo.assignment_submissions(assignment_id);
GO

CREATE TABLE dbo.student_course_grades (
    grade_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    course_id UNIQUEIDENTIFIER NOT NULL,
    student_id UNIQUEIDENTIFIER NOT NULL,
    assignments_score DECIMAL(5,2) NOT NULL DEFAULT 0,
    activities_score DECIMAL(5,2) NOT NULL DEFAULT 0,
    exams_score DECIMAL(5,2) NOT NULL DEFAULT 0,
    projects_score DECIMAL(5,2) NOT NULL DEFAULT 0,
    updated_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (course_id) REFERENCES dbo.courses(course_id),
    FOREIGN KEY (student_id) REFERENCES dbo.students(student_id),
    CONSTRAINT uq_course_student_grade UNIQUE (course_id, student_id)
);
GO

CREATE INDEX idx_student_course_grades_course ON dbo.student_course_grades(course_id);
GO

CREATE TABLE dbo.student_achievements (
    achievement_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    student_id UNIQUEIDENTIFIER NOT NULL,
    achievement_name NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX),
    awarded_by UNIQUEIDENTIFIER,
    awarded_date DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (student_id) REFERENCES dbo.students(student_id),
    FOREIGN KEY (awarded_by) REFERENCES dbo.users(user_id)
);
GO

CREATE INDEX idx_student_achievements_student ON dbo.student_achievements(student_id);
GO

CREATE TABLE dbo.announcements (
    announcement_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    title NVARCHAR(255) NOT NULL,
    content NVARCHAR(MAX) NOT NULL,
    author_id UNIQUEIDENTIFIER NOT NULL,
    course_id UNIQUEIDENTIFIER NULL,
    visibility NVARCHAR(50) NOT NULL DEFAULT 'all',
    is_published BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    created_by UNIQUEIDENTIFIER NOT NULL,
    updated_at DATETIME2,
    updated_by UNIQUEIDENTIFIER,
    FOREIGN KEY (author_id) REFERENCES dbo.users(user_id),
    FOREIGN KEY (created_by) REFERENCES dbo.users(user_id),
    FOREIGN KEY (updated_by) REFERENCES dbo.users(user_id),
    FOREIGN KEY (course_id) REFERENCES dbo.courses(course_id)
);
GO

CREATE INDEX idx_announcements_visibility ON dbo.announcements(visibility);
CREATE INDEX idx_announcements_created ON dbo.announcements(created_at DESC);
CREATE INDEX idx_announcements_course ON dbo.announcements(course_id);
GO

CREATE TABLE dbo.announcement_views (
    view_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    announcement_id UNIQUEIDENTIFIER NOT NULL,
    user_id UNIQUEIDENTIFIER NOT NULL,
    viewed_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (announcement_id) REFERENCES dbo.announcements(announcement_id),
    FOREIGN KEY (user_id) REFERENCES dbo.users(user_id)
);
GO

CREATE INDEX idx_announcement_views_announcement ON dbo.announcement_views(announcement_id);
CREATE INDEX idx_announcement_views_user ON dbo.announcement_views(user_id);
GO

CREATE TABLE dbo.messages (
    message_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    sender_id UNIQUEIDENTIFIER NOT NULL,
    receiver_id UNIQUEIDENTIFIER NOT NULL,
    content NVARCHAR(MAX) NOT NULL,
    is_read BIT NOT NULL DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (sender_id) REFERENCES dbo.users(user_id),
    FOREIGN KEY (receiver_id) REFERENCES dbo.users(user_id)
);
GO

CREATE INDEX idx_messages_sender ON dbo.messages(sender_id);
CREATE INDEX idx_messages_receiver ON dbo.messages(receiver_id);
GO

CREATE TABLE dbo.conversations (
    conversation_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    participant1_id UNIQUEIDENTIFIER NOT NULL,
    participant2_id UNIQUEIDENTIFIER NOT NULL,
    last_message_id UNIQUEIDENTIFIER,
    last_message_time DATETIME2,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (participant1_id) REFERENCES dbo.users(user_id),
    FOREIGN KEY (participant2_id) REFERENCES dbo.users(user_id),
    FOREIGN KEY (last_message_id) REFERENCES dbo.messages(message_id)
);
GO

CREATE INDEX idx_conversations_p1 ON dbo.conversations(participant1_id);
CREATE INDEX idx_conversations_p2 ON dbo.conversations(participant2_id);
GO

CREATE TABLE dbo.support_tickets (
    ticket_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    ticket_number NVARCHAR(50) NOT NULL UNIQUE,
    title NVARCHAR(255) NOT NULL,
    description NVARCHAR(MAX) NOT NULL,
    status NVARCHAR(50) NOT NULL DEFAULT 'open',
    priority NVARCHAR(50) NOT NULL DEFAULT 'medium',
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    created_by UNIQUEIDENTIFIER NOT NULL,
    updated_at DATETIME2,
    updated_by UNIQUEIDENTIFIER,
    student_id UNIQUEIDENTIFIER NOT NULL,
    assigned_to_id UNIQUEIDENTIFIER,
    FOREIGN KEY (created_by) REFERENCES dbo.users(user_id),
    FOREIGN KEY (updated_by) REFERENCES dbo.users(user_id),
    FOREIGN KEY (student_id) REFERENCES dbo.users(user_id),
    FOREIGN KEY (assigned_to_id) REFERENCES dbo.users(user_id)
);
GO

CREATE INDEX idx_support_tickets_student ON dbo.support_tickets(student_id);
CREATE INDEX idx_support_tickets_status ON dbo.support_tickets(status);
GO

CREATE TABLE dbo.ticket_comments (
    comment_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    ticket_id UNIQUEIDENTIFIER NOT NULL,
    user_id UNIQUEIDENTIFIER NOT NULL,
    content NVARCHAR(MAX) NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (ticket_id) REFERENCES dbo.support_tickets(ticket_id),
    FOREIGN KEY (user_id) REFERENCES dbo.users(user_id)
);
GO

CREATE INDEX idx_ticket_comments_ticket ON dbo.ticket_comments(ticket_id);
GO

------------------------------------------------------------
-- Audit Trail Infrastructure
------------------------------------------------------------

IF OBJECT_ID('dbo.audit_logs', 'U') IS NOT NULL
    DROP TABLE dbo.audit_logs;
GO

CREATE TABLE dbo.audit_logs (
    audit_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    table_name NVARCHAR(128) NOT NULL,
    record_primary_key NVARCHAR(200) NOT NULL,
    action NVARCHAR(10) NOT NULL,
    changed_by UNIQUEIDENTIFIER NULL,
    changed_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    new_data NVARCHAR(MAX) NULL,
    old_data NVARCHAR(MAX) NULL
);
GO

DECLARE @AuditTargets TABLE (
    TableName SYSNAME,
    PrimaryKeyColumn SYSNAME,
    ChangedByExpression NVARCHAR(400) NULL
);

INSERT INTO @AuditTargets (TableName, PrimaryKeyColumn, ChangedByExpression)
VALUES
    ('users', 'user_id', NULL),
    ('advisers', 'adviser_id', NULL),
    ('students', 'student_id', NULL),
    ('courses', 'course_id', 'COALESCE(i.created_by, d.created_by)'),
    ('student_courses', 'enrollment_id', NULL),
    ('class_assignments', 'assignment_id', 'COALESCE(i.created_by, d.created_by)'),
    ('assignment_submissions', 'submission_id', NULL),
    ('student_course_grades', 'grade_id', NULL),
    ('student_achievements', 'achievement_id', 'COALESCE(i.awarded_by, d.awarded_by)'),
    ('announcements', 'announcement_id', 'COALESCE(i.updated_by, i.created_by, d.updated_by, d.created_by)'),
    ('announcement_views', 'view_id', 'COALESCE(i.user_id, d.user_id)'),
    ('messages', 'message_id', 'COALESCE(i.sender_id, d.sender_id)'),
    ('conversations', 'conversation_id', NULL),
    ('support_tickets', 'ticket_id', 'COALESCE(i.updated_by, i.created_by, d.updated_by, d.created_by)'),
    ('ticket_comments', 'comment_id', 'COALESCE(i.user_id, d.user_id)');

DECLARE @TableName SYSNAME;
DECLARE @PrimaryKeyColumn SYSNAME;
DECLARE @ChangedByExpression NVARCHAR(400);
DECLARE @CreateTriggerSql NVARCHAR(MAX);
DECLARE @QuotedTable NVARCHAR(260);
DECLARE @TriggerName NVARCHAR(260);

DECLARE audit_cursor CURSOR FOR
SELECT TableName, PrimaryKeyColumn, ChangedByExpression
FROM @AuditTargets;

OPEN audit_cursor;
FETCH NEXT FROM audit_cursor INTO @TableName, @PrimaryKeyColumn, @ChangedByExpression;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @QuotedTable = QUOTENAME(@TableName);
    SET @TriggerName = QUOTENAME('trg_' + @TableName + '_Audit');

    SET @CreateTriggerSql = N'
    CREATE OR ALTER TRIGGER dbo.' + @TriggerName + N'
    ON dbo.' + @QuotedTable + N'
    AFTER INSERT, UPDATE, DELETE
    AS
    BEGIN
        SET NOCOUNT ON;

        INSERT INTO dbo.audit_logs (
            audit_id,
            table_name,
            record_primary_key,
            action,
            changed_by,
            changed_at,
            new_data,
            old_data
        )
        SELECT
            NEWID(),
            ''' + @TableName + ''',
            CONVERT(NVARCHAR(200), COALESCE(i.' + QUOTENAME(@PrimaryKeyColumn, N'[') + ', d.' + QUOTENAME(@PrimaryKeyColumn, N'[') + ')),
            CASE
                WHEN i.' + QUOTENAME(@PrimaryKeyColumn, N'[') + ' IS NOT NULL AND d.' + QUOTENAME(@PrimaryKeyColumn, N'[') + ' IS NOT NULL THEN ''UPDATE''
                WHEN i.' + QUOTENAME(@PrimaryKeyColumn, N'[') + ' IS NOT NULL THEN ''INSERT''
                ELSE ''DELETE''
            END,
            ' + COALESCE(@ChangedByExpression, 'NULL') + ',
            GETUTCDATE(),
            CASE WHEN i.' + QUOTENAME(@PrimaryKeyColumn, N'[') + ' IS NOT NULL THEN (SELECT i.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END,
            CASE WHEN d.' + QUOTENAME(@PrimaryKeyColumn, N'[') + ' IS NOT NULL THEN (SELECT d.* FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) END
        FROM inserted i
        FULL OUTER JOIN deleted d
            ON i.' + QUOTENAME(@PrimaryKeyColumn, N'[') + ' = d.' + QUOTENAME(@PrimaryKeyColumn, N'[') + ';
    END;';

    EXEC sp_executesql @CreateTriggerSql;

    FETCH NEXT FROM audit_cursor INTO @TableName, @PrimaryKeyColumn, @ChangedByExpression;
END

CLOSE audit_cursor;
DEALLOCATE audit_cursor;
GO

------------------------------------------------------------
-- Deterministic System Accounts
------------------------------------------------------------

DECLARE @Now DATETIME2 = GETUTCDATE();
DECLARE @AdminId UNIQUEIDENTIFIER = 'F5E0A1E7-7C77-4A04-9E68-7B7F7A7D0001';
DECLARE @TeacherPrimaryId UNIQUEIDENTIFIER = 'F5E0A1E7-7C77-4A04-9E68-7B7F7A7D0002';
DECLARE @TeacherSecondaryId UNIQUEIDENTIFIER = 'F5E0A1E7-7C77-4A04-9E68-7B7F7A7D0003';
DECLARE @StudentPrimaryId UNIQUEIDENTIFIER = 'F5E0A1E7-7C77-4A04-9E68-7B7F7A7D0004';

INSERT INTO dbo.users (user_id, email, password_hash, password_salt, role, display_name, phone_number, address, status, archive_reason, created_at, created_by, updated_at, updated_by)
VALUES
    (@AdminId, 'admin@university.edu', '+9/zNWABQWmUz7vYKwkG0wPfoZCxZ9g2OnnM6r1EvqI=', 'gvVIA8tmIqly64N8Zqt1zg==', 'Admin', 'Admin User', '+1 (555) 345-6789', 'Administration Building, Room 100', 'active', NULL, @Now, NULL, NULL, NULL),
    (@TeacherPrimaryId, 'teacher@university.edu', 'YM5TCtG/TtHZMmqP2YllL1IRlBw4m8ny01ESx3atldQ=', 'pMoH0ATf/TwF8GKJetrbyA==', 'Teacher', 'John Teacher', '+1 (555) 234-5678', 'Faculty Building, Office 301', 'active', NULL, @Now, @AdminId, NULL, NULL),
    (@TeacherSecondaryId, 'adviser@university.edu', 'YM5TCtG/TtHZMmqP2YllL1IRlBw4m8ny01ESx3atldQ=', 'pMoH0ATf/TwF8GKJetrbyA==', 'Teacher', 'Priya Shah', '+1 (555) 210-7788', 'Faculty Building, Office 302', 'active', NULL, @Now, @AdminId, NULL, NULL),
    (@StudentPrimaryId, 'student@university.edu', '2Dyfiau1pDwzyQxBQCpHWri9i7pmcQVJBLtnLW/g+ag=', 'sYCDlcmAoN56njDjs9uTag==', 'Student', 'Sarah Student', '+1 (555) 543-2100', 'North Dorm, Room 410', 'active', NULL, @Now, @TeacherPrimaryId, NULL, NULL);

INSERT INTO dbo.advisers (adviser_id, department, office_location, consultation_hours)
VALUES
    (@TeacherPrimaryId, 'Computer Science', 'Faculty Building, Office 301', 'T/Th 2PM-5PM'),
    (@TeacherSecondaryId, 'Information Technology', 'Faculty Building, Office 302', 'W/F 10AM-1PM');

INSERT INTO dbo.students (student_id, student_number, program, year_level, gpa, status, adviser_id, created_at)
VALUES (@StudentPrimaryId, 'STU-2024-0001', 'Bachelor of Science in Computer Science', 'Year 3', 3.85, 'active', @TeacherPrimaryId, @Now);

INSERT INTO dbo.student_achievements (achievement_id, student_id, achievement_name, description, awarded_by, awarded_date)
VALUES
    (NEWID(), @StudentPrimaryId, 'Dean''s List', 'Achieved Dean''s List recognition for academic excellence in Fall 2024', @TeacherPrimaryId, DATEADD(MONTH, -2, @Now)),
    (NEWID(), @StudentPrimaryId, 'Research Grant', 'Awarded research grant for innovative AI project proposal', @TeacherPrimaryId, DATEADD(MONTH, -3, @Now)),
    (NEWID(), @StudentPrimaryId, 'Hackathon Winner', 'First place winner in University Hackathon 2024', @AdminId, DATEADD(MONTH, -1, @Now));

------------------------------------------------------------
-- Sample Conversations for Primary Student/Teacher
------------------------------------------------------------

DECLARE @Msg1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Msg2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Msg3 UNIQUEIDENTIFIER = NEWID();
DECLARE @Msg4 UNIQUEIDENTIFIER = NEWID();

INSERT INTO dbo.messages (message_id, sender_id, receiver_id, content, is_read, created_at)
VALUES
    (@Msg1, @TeacherPrimaryId, @StudentPrimaryId, 'Hi Sarah, I''ve reviewed your thesis draft.', 0, DATEADD(MINUTE, -30, @Now)),
    (@Msg2, @TeacherPrimaryId, @StudentPrimaryId, 'Overall it''s excellent work, just a few minor suggestions on the methodology section.', 0, DATEADD(MINUTE, -28, @Now)),
    (@Msg3, @StudentPrimaryId, @TeacherPrimaryId, 'Thank you! I would love to hear your feedback.', 1, DATEADD(MINUTE, -20, @Now)),
    (@Msg4, @StudentPrimaryId, @TeacherPrimaryId, 'Could we meet Thursday afternoon?', 1, DATEADD(MINUTE, -15, @Now));

INSERT INTO dbo.conversations (conversation_id, participant1_id, participant2_id, last_message_id, last_message_time, created_at)
VALUES (NEWID(), @StudentPrimaryId, @TeacherPrimaryId, @Msg4, DATEADD(MINUTE, -15, @Now), DATEADD(DAY, -5, @Now));

------------------------------------------------------------
-- Support Ticket Analytics Seed
------------------------------------------------------------

DECLARE @TicketBaseline DATETIME2 = DATEADD(DAY, -30, @Now);
DECLARE @TicketStudent UNIQUEIDENTIFIER = @StudentPrimaryId;
DECLARE @TicketAdvisor UNIQUEIDENTIFIER = @TeacherPrimaryId;

INSERT INTO dbo.support_tickets (
    ticket_id, ticket_number, title, description, status, priority,
    created_at, created_by, updated_at, updated_by, student_id, assigned_to_id)
VALUES
    (NEWID(), 'TKT-2024-0001', 'Portal login issue', 'Student cannot login after password reset.',
        'resolved', 'high',
        DATEADD(DAY, -28, @Now), @TicketStudent,
        DATEADD(MINUTE, 120, DATEADD(DAY, -28, @Now)), @TicketAdvisor,
        @TicketStudent, @TicketAdvisor),
    (NEWID(), 'TKT-2024-0002', 'Scholarship document upload', 'Upload button disabled on Chrome browser.',
        'resolved', 'medium',
        DATEADD(DAY, -20, @Now), @TicketStudent,
        DATEADD(MINUTE, 270, DATEADD(DAY, -20, @Now)), @TicketAdvisor,
        @TicketStudent, @TicketAdvisor),
    (NEWID(), 'TKT-2024-0003', 'Advising schedule conflict', 'Need to rebook advising session.',
        'resolved', 'low',
        DATEADD(DAY, -15, @Now), @TicketStudent,
        DATEADD(MINUTE, 75, DATEADD(DAY, -14, @Now)), @TicketAdvisor,
        @TicketStudent, @TicketAdvisor),
    (NEWID(), 'TKT-2024-0004', 'Grading discrepancy', 'Grade not appearing in portal.',
        'in_progress', 'high',
        DATEADD(DAY, -10, @Now), @TicketStudent,
        DATEADD(MINUTE, 405, DATEADD(DAY, -9, @Now)), @TicketAdvisor,
        @TicketStudent, @TicketAdvisor),
    (NEWID(), 'TKT-2024-0005', 'Dorm access badge replacement', 'Lost RFID badge, needs replacement.',
        'resolved', 'medium',
        DATEADD(DAY, -7, @Now), @TicketStudent,
        DATEADD(MINUTE, 190, DATEADD(DAY, -7, @Now)), @TicketAdvisor,
        @TicketStudent, @TicketAdvisor),
    (NEWID(), 'TKT-2024-0006', 'Tuition payment confirmation', 'Payment not reflected yet.',
        'resolved', 'high',
        DATEADD(DAY, -5, @Now), @TicketStudent,
        DATEADD(MINUTE, 300, DATEADD(DAY, -5, @Now)), @TicketAdvisor,
        @TicketStudent, @TicketAdvisor),
    (NEWID(), 'TKT-2024-0007', 'Virtual lab access', 'Cannot open virtual lab environment.',
        'open', 'medium',
        DATEADD(DAY, -3, @Now), @TicketStudent,
        NULL, NULL,
        @TicketStudent, @TicketAdvisor),
    (NEWID(), 'TKT-2024-0008', 'Adviser reassignment request', 'Student requesting new adviser for capstone.',
        'resolved', 'low',
        DATEADD(DAY, -2, @Now), @TicketStudent,
        DATEADD(MINUTE, 440, DATEADD(DAY, -2, @Now)), @TicketAdvisor,
        @TicketStudent, @TicketAdvisor);

------------------------------------------------------------
-- Additional Message Activity for Analytics
------------------------------------------------------------

INSERT INTO dbo.messages (message_id, sender_id, receiver_id, content, is_read, created_at)
VALUES
    (NEWID(), @TeacherPrimaryId, @StudentPrimaryId, 'Reminder: submit your lab report by Friday.', 0, DATEADD(DAY, -6, @Now)),
    (NEWID(), @StudentPrimaryId, @TeacherPrimaryId, 'Noted, will upload it tomorrow.', 1, DATEADD(MINUTE, 60, DATEADD(DAY, -6, @Now))),
    (NEWID(), @TeacherPrimaryId, @StudentPrimaryId, 'Here is the updated advising checklist.', 0, DATEADD(DAY, -4, @Now)),
    (NEWID(), @StudentPrimaryId, @TeacherPrimaryId, 'Thanks! Also sent you the presentation slides.', 1, DATEADD(MINUTE, 45, DATEADD(DAY, -4, @Now))),
    (NEWID(), @TeacherPrimaryId, @StudentPrimaryId, 'Great progress on your capstone draft.', 0, DATEADD(DAY, -1, @Now));

------------------------------------------------------------
-- Announcement Samples
------------------------------------------------------------

INSERT INTO dbo.announcements (announcement_id, title, content, author_id, visibility, is_published, created_at, created_by)
VALUES
    (NEWID(), 'System Maintenance', 'We will be performing system maintenance on Saturday at 10PM.', @AdminId, 'all', 1, DATEADD(DAY, -1, @Now), @AdminId),
    (NEWID(), 'Advising Week', 'Please book your advising slots before finals week.', @TeacherPrimaryId, 'students', 1, DATEADD(DAY, -2, @Now), @TeacherPrimaryId),
    (NEWID(), 'Faculty Sync', 'Advisers meeting on Friday, 2PM in the faculty lounge.', @TeacherSecondaryId, 'advisers', 1, DATEADD(DAY, -3, @Now), @TeacherSecondaryId);

------------------------------------------------------------
-- Bulk Demo Seed: 10 Teachers × 3 Classes × 30 Students
------------------------------------------------------------

DECLARE @TeacherCount INT = 10;
DECLARE @ClassesPerTeacher INT = 3;
DECLARE @StudentsPerClass INT = 30;
DECLARE @TeacherIndex INT = 1;

WHILE @TeacherIndex <= @TeacherCount
BEGIN
    DECLARE @TeacherUserId UNIQUEIDENTIFIER = NEWID();
    DECLARE @TeacherEmail NVARCHAR(255) = CONCAT('teacher', FORMAT(@TeacherIndex, '00'), '@university.edu');
    DECLARE @TeacherDisplayName NVARCHAR(255) = CONCAT('Teacher ', @TeacherIndex, ' Rivera');
    DECLARE @TeacherDepartment NVARCHAR(255) =
        CASE ((@TeacherIndex - 1) % 3)
            WHEN 0 THEN 'Computer Science'
            WHEN 1 THEN 'Information Technology'
            ELSE 'Software Engineering'
        END;

    INSERT INTO dbo.users (user_id, email, password_hash, password_salt, role, display_name, phone_number, address, status, archive_reason, created_at, created_by, updated_at, updated_by)
    VALUES (
        @TeacherUserId,
        @TeacherEmail,
        'YM5TCtG/TtHZMmqP2YllL1IRlBw4m8ny01ESx3atldQ=',
        'pMoH0ATf/TwF8GKJetrbyA==',
        'Teacher',
        @TeacherDisplayName,
        CONCAT('+1 (555) ', FORMAT(2300 + @TeacherIndex, '000'), '-890'),
        CONCAT('Faculty Building, Office ', FORMAT(200 + @TeacherIndex, '000')),
        'active',
        NULL,
        @Now,
        @AdminId,
        NULL,
        NULL
    );

    INSERT INTO dbo.advisers (adviser_id, department, office_location, consultation_hours)
    VALUES (@TeacherUserId, @TeacherDepartment, CONCAT('Faculty Building, Office ', FORMAT(200 + @TeacherIndex, '000')), 'MWF 9AM-12PM');

    DECLARE @ClassIndex INT = 1;

    WHILE @ClassIndex <= @ClassesPerTeacher
    BEGIN
        DECLARE @CourseId UNIQUEIDENTIFIER = NEWID();
        DECLARE @CourseCode NVARCHAR(50) = CONCAT('CS', FORMAT(@TeacherIndex, '00'), FORMAT(@ClassIndex, '0'), '0');
        DECLARE @CourseName NVARCHAR(255) = CONCAT('Advanced Topic ', @ClassIndex, ' - Cohort ', @TeacherIndex);
        DECLARE @Schedule NVARCHAR(255) = CONCAT('MWF ', 8 + @ClassIndex, ':00 AM - ', 9 + @ClassIndex, ':15 AM');

        INSERT INTO dbo.courses (course_id, course_code, course_name, credits, schedule, professor_name, created_at, created_by)
        VALUES (@CourseId, @CourseCode, @CourseName, 3, @Schedule, @TeacherDisplayName, @Now, @TeacherUserId);

        DECLARE @AssignmentIntroId UNIQUEIDENTIFIER = NEWID();
        DECLARE @AssignmentProjectId UNIQUEIDENTIFIER = NEWID();

        INSERT INTO dbo.class_assignments (assignment_id, course_id, title, description, deadline, total_points, created_by, created_at)
        VALUES
            (@AssignmentIntroId, @CourseId,
                CONCAT('Kickoff Brief - ', @CourseCode),
                'Submit a one-page intent brief outlining your goals for this course.',
                DATEADD(DAY, @ClassIndex + 5, @Now),
                50,
                @TeacherUserId,
                @Now),
            (@AssignmentProjectId, @CourseId,
                CONCAT('Capstone Sprint ', @ClassIndex, ' Update'),
                'Upload your prototype plus reflection on the latest sprint learnings.',
                DATEADD(DAY, @ClassIndex + 12, @Now),
                100,
                @TeacherUserId,
                @Now);

        INSERT INTO dbo.announcements (announcement_id, title, content, author_id, course_id, visibility, is_published, created_at, created_by)
        VALUES (
            NEWID(),
            CONCAT('Reminder • ', @CourseCode),
            'Weekly stand-up on Wednesday. Bring blockers and assignment questions.',
            @TeacherUserId,
            @CourseId,
            'students',
            1,
            DATEADD(DAY, -@ClassIndex, @Now),
            @TeacherUserId
        );

        DECLARE @StudentIndex INT = 1;

        WHILE @StudentIndex <= @StudentsPerClass
        BEGIN
            DECLARE @GlobalStudentIndex INT = ((@TeacherIndex - 1) * @ClassesPerTeacher * @StudentsPerClass) + ((@ClassIndex - 1) * @StudentsPerClass) + @StudentIndex;
            DECLARE @StudentUserId UNIQUEIDENTIFIER = NEWID();
            DECLARE @StudentEmail NVARCHAR(255) = CONCAT('student', FORMAT(@TeacherIndex, '00'), FORMAT(@ClassIndex, '0'), FORMAT(@StudentIndex, '00'), '@university.edu');
            DECLARE @StudentName NVARCHAR(255) = CONCAT('Student ', @TeacherIndex, '-', @ClassIndex, '-', @StudentIndex);
            DECLARE @StudentNumber NVARCHAR(50) = CONCAT('STU-', FORMAT(@TeacherIndex, '00'), FORMAT(@ClassIndex, '0'), '-', FORMAT(@StudentIndex, '0000'));
            DECLARE @Program NVARCHAR(255) =
                CASE ((@ClassIndex - 1) % 3)
                    WHEN 0 THEN 'BS Computer Science'
                    WHEN 1 THEN 'BS Information Technology'
                    ELSE 'BS Software Engineering'
                END;

            INSERT INTO dbo.users (user_id, email, password_hash, password_salt, role, display_name, phone_number, address, status, archive_reason, created_at, created_by, updated_at, updated_by)
            VALUES (
                @StudentUserId,
                @StudentEmail,
                '2Dyfiau1pDwzyQxBQCpHWri9i7pmcQVJBLtnLW/g+ag=',
                'sYCDlcmAoN56njDjs9uTag==',
                'Student',
                @StudentName,
                CONCAT('+1 (555) ', FORMAT(3100 + @StudentIndex, '000'), '-', FORMAT(7000 + @ClassIndex, '0000')),
                CONCAT('Dorm ', CHAR(64 + @ClassIndex), ', Room ', FORMAT(@StudentIndex, '000')),
                'active',
                NULL,
                @Now,
                @TeacherUserId,
                NULL,
                NULL
            );

            INSERT INTO dbo.students (student_id, student_number, program, year_level, gpa, status, adviser_id, created_at)
            VALUES (
                @StudentUserId,
                @StudentNumber,
                @Program,
                CONCAT('Year ', ((@StudentIndex - 1) % 4) + 1),
                CAST(ROUND(2.75 + (@StudentIndex % 20) * 0.05, 2) AS DECIMAL(3,2)),
                'active',
                @TeacherUserId,
                @Now
            );

            INSERT INTO dbo.student_course_grades (grade_id, course_id, student_id, assignments_score, activities_score, exams_score, projects_score, updated_at)
            VALUES (
                NEWID(),
                @CourseId,
                @StudentUserId,
                CAST(70 + (@StudentIndex % 25) AS DECIMAL(5,2)),
                CAST(68 + (@StudentIndex % 20) AS DECIMAL(5,2)),
                CAST(72 + (@StudentIndex % 18) AS DECIMAL(5,2)),
                CAST(75 + (@StudentIndex % 22) AS DECIMAL(5,2)),
                DATEADD(DAY, -(@StudentIndex % 5), @Now)
            );

            IF @StudentIndex % 5 <> 0
            BEGIN
                INSERT INTO dbo.assignment_submissions (submission_id, assignment_id, student_id, submitted_at, score, status, notes, submission_content)
                VALUES (
                    NEWID(),
                    @AssignmentIntroId,
                    @StudentUserId,
                    DATEADD(DAY, -(@StudentIndex % 3), @Now),
                    30 + (@StudentIndex % 21),
                    'graded',
                    'Good work on the introduction brief!',
                    'Attached is my one-page brief outlining the project goals, key constraints, and team structure for this course.'
                );
            END

            IF @StudentIndex % 3 <> 0
            BEGIN
                INSERT INTO dbo.assignment_submissions (submission_id, assignment_id, student_id, submitted_at, score, status, notes, submission_content)
                VALUES (
                    NEWID(),
                    @AssignmentProjectId,
                    @StudentUserId,
                    DATEADD(DAY, -(@StudentIndex % 2), @Now),
                    70 + (@StudentIndex % 26),
                    'graded',
                    'Excellent analysis and proposed improvements!',
                    'Case Study Analysis: I have reviewed the assigned case study and identified key areas for improvement. My two proposals focus on optimizing the development workflow and enhancing user experience.'
                );
            END

            IF @StudentIndex <= 2
            BEGIN
                DECLARE @MessageA UNIQUEIDENTIFIER = NEWID();
                DECLARE @MessageB UNIQUEIDENTIFIER = NEWID();
                DECLARE @ConversationId UNIQUEIDENTIFIER = NEWID();

                INSERT INTO dbo.messages (message_id, sender_id, receiver_id, content, is_read, created_at)
                VALUES
                    (@MessageA, @TeacherUserId, @StudentUserId, CONCAT('Hello ', @StudentName, ', welcome to ', @CourseCode, '!'), 0, DATEADD(MINUTE, -15 * @StudentIndex, @Now)),
                    (@MessageB, @StudentUserId, @TeacherUserId, 'Thank you professor! Looking forward to the class.', 0, DATEADD(MINUTE, -10 * @StudentIndex, @Now));

                INSERT INTO dbo.conversations (conversation_id, participant1_id, participant2_id, last_message_id, last_message_time, created_at)
                VALUES (
                    @ConversationId,
                    @TeacherUserId,
                    @StudentUserId,
                    @MessageB,
                    DATEADD(MINUTE, -10 * @StudentIndex, @Now),
                    @Now
                );
            END

            SET @StudentIndex += 1;
        END

        SET @ClassIndex += 1;
    END

    ;WITH TeacherStudents AS (
        SELECT s.student_id
        FROM dbo.students s
        WHERE s.adviser_id = @TeacherUserId
    ),
    TeacherCourses AS (
        SELECT course_id, created_by,
               ROW_NUMBER() OVER (PARTITION BY created_by ORDER BY course_code) AS course_num
        FROM dbo.courses
        WHERE created_by = @TeacherUserId
    )
    INSERT INTO dbo.student_courses (enrollment_id, student_id, course_id, teacher_id, enrolled_at)
    SELECT NEWID(), ts.student_id, tc.course_id, tc.created_by, @Now
    FROM TeacherStudents ts
    CROSS JOIN TeacherCourses tc
    LEFT JOIN dbo.student_courses existing
        ON existing.student_id = ts.student_id
       AND existing.course_id = tc.course_id
    WHERE existing.enrollment_id IS NULL;

    SET @TeacherIndex += 1;
END

-- Create grades for all enrolled students that don't have grades yet
INSERT INTO dbo.student_course_grades (grade_id, course_id, student_id, assignments_score, activities_score, exams_score, projects_score, updated_at)
SELECT 
    NEWID(),
    sc.course_id,
    sc.student_id,
    CAST(CASE 
        WHEN ABS(CHECKSUM(sc.student_id, sc.course_id)) % 180 IN (2, 8, 15, 22, 28) THEN 35 + (ABS(CHECKSUM(sc.student_id)) % 20)
        ELSE 70 + (ABS(CHECKSUM(sc.student_id)) % 25)
    END AS DECIMAL(5,2)),
    CAST(CASE 
        WHEN ABS(CHECKSUM(sc.student_id, sc.course_id)) % 180 IN (2, 8, 15, 22, 28) THEN 30 + (ABS(CHECKSUM(sc.student_id)) % 20)
        ELSE 68 + (ABS(CHECKSUM(sc.student_id)) % 20)
    END AS DECIMAL(5,2)),
    CAST(CASE 
        WHEN ABS(CHECKSUM(sc.student_id, sc.course_id)) % 180 IN (2, 8, 15, 22, 28) THEN 40 + (ABS(CHECKSUM(sc.student_id)) % 20)
        ELSE 72 + (ABS(CHECKSUM(sc.student_id)) % 18)
    END AS DECIMAL(5,2)),
    CAST(CASE 
        WHEN ABS(CHECKSUM(sc.student_id, sc.course_id)) % 180 IN (2, 8, 15, 22, 28) THEN 38 + (ABS(CHECKSUM(sc.student_id)) % 20)
        ELSE 75 + (ABS(CHECKSUM(sc.student_id)) % 22)
    END AS DECIMAL(5,2)),
    @Now
FROM dbo.student_courses sc
LEFT JOIN dbo.student_course_grades scg ON sc.course_id = scg.course_id AND sc.student_id = scg.student_id
WHERE scg.grade_id IS NULL;

------------------------------------------------------------
-- Seed historical announcement view data for charts
------------------------------------------------------------
DECLARE @MonthsOfHistory INT = 6;

;WITH RecentAnnouncements AS (
    SELECT TOP 12 announcement_id, created_at
    FROM dbo.announcements
    ORDER BY created_at DESC
),
SampleStudents AS (
    SELECT TOP 25 user_id, created_at
    FROM dbo.users
    WHERE role = 'Student'
    ORDER BY created_at DESC
),
MonthOffsets AS (
    SELECT 0 AS OffsetValue
    UNION ALL
    SELECT OffsetValue + 1
    FROM MonthOffsets
    WHERE OffsetValue + 1 < @MonthsOfHistory
)
INSERT INTO dbo.announcement_views (view_id, announcement_id, user_id, viewed_at)
SELECT TOP (600)
    NEWID(),
    ra.announcement_id,
    ss.user_id,
    DATEADD(
        DAY,
        ABS(CHECKSUM(ra.announcement_id, ss.user_id, mo.OffsetValue)) % 28,
        DATEFROMPARTS(
            YEAR(DATEADD(MONTH, -mo.OffsetValue, @Now)),
            MONTH(DATEADD(MONTH, -mo.OffsetValue, @Now)),
            1
        )
    )
FROM RecentAnnouncements ra
CROSS JOIN SampleStudents ss
CROSS JOIN MonthOffsets mo
WHERE ABS(CHECKSUM(ra.announcement_id, ss.user_id, mo.OffsetValue)) % 5 = 0
ORDER BY ra.created_at DESC, mo.OffsetValue, ss.created_at DESC
OPTION (MAXRECURSION 100);

------------------------------------------------------------
-- Specific Seeding for student03324@university.edu
------------------------------------------------------------

-- Get the actual user_id for student03324@university.edu (created during bulk seeding)
DECLARE @TargetStudentId UNIQUEIDENTIFIER;
SELECT @TargetStudentId = user_id FROM dbo.users WHERE email = 'student03324@university.edu';

-- If student doesn't exist, skip this section
IF @TargetStudentId IS NULL
BEGIN
    PRINT 'Warning: student03324@university.edu not found in database. Skipping student-specific seeding.';
END
ELSE
BEGIN
    DECLARE @Teacher1Id UNIQUEIDENTIFIER = 'F5E0A1E7-7C77-4A04-9E68-7B7F7A7D0002';
    DECLARE @Teacher2Id UNIQUEIDENTIFIER = 'F5E0A1E7-7C77-4A04-9E68-7B7F7A7D0003';
    DECLARE @Teacher3Id UNIQUEIDENTIFIER = (SELECT TOP 1 adviser_id FROM dbo.advisers WHERE adviser_id != @Teacher1Id AND adviser_id != @Teacher2Id ORDER BY NEWID());
    DECLARE @Teacher4Id UNIQUEIDENTIFIER = (SELECT TOP 1 adviser_id FROM dbo.advisers WHERE adviser_id NOT IN (@Teacher1Id, @Teacher2Id, @Teacher3Id) ORDER BY NEWID());

-- Update user display names
UPDATE dbo.users SET display_name = 'Maria Santos' WHERE user_id = @TargetStudentId;
UPDATE dbo.users SET display_name = 'Dr. James Mitchell' WHERE user_id = @Teacher1Id;
UPDATE dbo.users SET display_name = 'Prof. Sarah Chen' WHERE user_id = @Teacher2Id;
UPDATE dbo.users SET display_name = 'Dr. Robert Williams' WHERE user_id = @Teacher3Id;
UPDATE dbo.users SET display_name = 'Prof. Elena Rodriguez' WHERE user_id = @Teacher4Id;

-- Ensure student record exists
IF NOT EXISTS (SELECT 1 FROM dbo.students WHERE student_id = @TargetStudentId)
BEGIN
    INSERT INTO dbo.students (student_id, student_number, program, year_level, gpa, status, adviser_id, created_at)
    VALUES (@TargetStudentId, 'STU-2024-3324', 'BS Computer Science', 'Year 3', 3.75, 'active', @Teacher1Id, @Now);
END

-- Remove all existing enrollments for this student first
DELETE FROM dbo.student_courses WHERE student_id = @TargetStudentId;

-- Enroll student in exactly 4 courses
INSERT INTO dbo.student_courses (enrollment_id, student_id, course_id, teacher_id, enrolled_at)
SELECT TOP 4 NEWID(), @TargetStudentId, c.course_id, c.created_by, @Now
FROM dbo.courses c
ORDER BY NEWID();

-- Add Student Achievements
INSERT INTO dbo.student_achievements (achievement_id, student_id, achievement_name, description, awarded_by, awarded_date)
VALUES
    (NEWID(), @TargetStudentId, 'Perfect Attendance', 'Maintained perfect attendance throughout the semester', @Teacher1Id, DATEADD(MONTH, -2, @Now)),
    (NEWID(), @TargetStudentId, 'Excellent Project Work', 'Demonstrated exceptional skills in capstone project', @Teacher2Id, DATEADD(MONTH, -1, @Now)),
    (NEWID(), @TargetStudentId, 'Class Participation Award', 'Active contributor in class discussions and activities', @Teacher3Id, DATEADD(WEEK, -3, @Now)),
    (NEWID(), @TargetStudentId, 'Peer Mentor', 'Helped fellow students with course material and assignments', @Teacher4Id, DATEADD(WEEK, -1, @Now));

-- Create 4 Conversations with Teachers
DECLARE @TargetMsg1 UNIQUEIDENTIFIER = NEWID();
DECLARE @TargetMsg2 UNIQUEIDENTIFIER = NEWID();
DECLARE @TargetMsg3 UNIQUEIDENTIFIER = NEWID();
DECLARE @TargetMsg4 UNIQUEIDENTIFIER = NEWID();
DECLARE @TargetMsg5 UNIQUEIDENTIFIER = NEWID();
DECLARE @TargetMsg6 UNIQUEIDENTIFIER = NEWID();
DECLARE @TargetMsg7 UNIQUEIDENTIFIER = NEWID();
DECLARE @TargetMsg8 UNIQUEIDENTIFIER = NEWID();

-- Conversation 1: With Teacher 1
INSERT INTO dbo.messages (message_id, sender_id, receiver_id, content, is_read, created_at)
VALUES
    (@TargetMsg1, @Teacher1Id, @TargetStudentId, 'Hi! I wanted to discuss your project proposal. Great work so far!', 0, DATEADD(DAY, -7, @Now)),
    (@TargetMsg2, @TargetStudentId, @Teacher1Id, 'Thank you! I would love your feedback on the implementation approach.', 1, DATEADD(DAY, -6, @Now));

INSERT INTO dbo.conversations (conversation_id, participant1_id, participant2_id, last_message_id, last_message_time, created_at)
VALUES (NEWID(), @Teacher1Id, @TargetStudentId, @TargetMsg2, DATEADD(DAY, -6, @Now), DATEADD(DAY, -7, @Now));

-- Conversation 2: With Teacher 2
INSERT INTO dbo.messages (message_id, sender_id, receiver_id, content, is_read, created_at)
VALUES
    (@TargetMsg3, @Teacher2Id, @TargetStudentId, 'Your assignment submission was excellent. Keep up the good work!', 0, DATEADD(DAY, -5, @Now)),
    (@TargetMsg4, @TargetStudentId, @Teacher2Id, 'Thank you for the positive feedback! I really enjoyed this assignment.', 1, DATEADD(DAY, -4, @Now));

INSERT INTO dbo.conversations (conversation_id, participant1_id, participant2_id, last_message_id, last_message_time, created_at)
VALUES (NEWID(), @Teacher2Id, @TargetStudentId, @TargetMsg4, DATEADD(DAY, -4, @Now), DATEADD(DAY, -5, @Now));

-- Conversation 3: With Teacher 3
INSERT INTO dbo.messages (message_id, sender_id, receiver_id, content, is_read, created_at)
VALUES
    (@TargetMsg5, @TargetStudentId, @Teacher3Id, 'Can we schedule a meeting to discuss the upcoming exam?', 1, DATEADD(DAY, -3, @Now)),
    (@TargetMsg6, @Teacher3Id, @TargetStudentId, 'Absolutely! How about Thursday at 2 PM in my office?', 0, DATEADD(DAY, -2, @Now));

INSERT INTO dbo.conversations (conversation_id, participant1_id, participant2_id, last_message_id, last_message_time, created_at)
VALUES (NEWID(), @TargetStudentId, @Teacher3Id, @TargetMsg6, DATEADD(DAY, -2, @Now), DATEADD(DAY, -3, @Now));

-- Conversation 4: With Teacher 4
INSERT INTO dbo.messages (message_id, sender_id, receiver_id, content, is_read, created_at)
VALUES
    (@TargetMsg7, @Teacher4Id, @TargetStudentId, 'I noticed you helping other students with the lab work. Great initiative!', 0, DATEADD(DAY, -1, @Now)),
    (@TargetMsg8, @TargetStudentId, @Teacher4Id, 'Thank you! I enjoy helping my classmates understand the concepts better.', 1, DATEADD(HOUR, -12, @Now));

INSERT INTO dbo.conversations (conversation_id, participant1_id, participant2_id, last_message_id, last_message_time, created_at)
VALUES (NEWID(), @Teacher4Id, @TargetStudentId, @TargetMsg8, DATEADD(HOUR, -12, @Now), DATEADD(DAY, -1, @Now));

-- Create 5 Support Tickets
INSERT INTO dbo.support_tickets (ticket_id, ticket_number, title, description, status, created_at, created_by, updated_at, updated_by, student_id, assigned_to_id)
VALUES
    (NEWID(), 'TKT-2024-0101', 'Course Registration Issue', 'Unable to register for Spring 2025 courses due to system error.', 'resolved', DATEADD(DAY, -20, @Now), @TargetStudentId, DATEADD(DAY, -18, @Now), @Teacher1Id, @TargetStudentId, @Teacher1Id),
    (NEWID(), 'TKT-2024-0102', 'Grade Appeal', 'Requesting review of midterm exam grade - believe there was a calculation error.', 'in_progress', DATEADD(DAY, -15, @Now), @TargetStudentId, DATEADD(DAY, -10, @Now), @Teacher2Id, @TargetStudentId, @Teacher2Id),
    (NEWID(), 'TKT-2024-0103', 'Lab Equipment Access', 'Need access to computer lab for capstone project work.', 'resolved', DATEADD(DAY, -12, @Now), @TargetStudentId, DATEADD(DAY, -11, @Now), @Teacher3Id, @TargetStudentId, @Teacher3Id),
    (NEWID(), 'TKT-2024-0104', 'Transcript Request', 'Official transcript needed for graduate school application.', 'resolved', DATEADD(DAY, -8, @Now), @TargetStudentId, DATEADD(DAY, -7, @Now), @Teacher4Id, @TargetStudentId, @Teacher4Id),
    (NEWID(), 'TKT-2024-0105', 'Assignment Extension Request', 'Requesting 2-day extension for final project due to illness.', 'open', DATEADD(DAY, -2, @Now), @TargetStudentId, NULL, NULL, @TargetStudentId, @Teacher1Id);

-- Add Course Grades for Student 03324 (All enrolled courses with realistic grades)
DECLARE @CourseIds TABLE (course_id UNIQUEIDENTIFIER, course_name NVARCHAR(255));
INSERT INTO @CourseIds
SELECT c.course_id, c.course_name
FROM dbo.student_courses sc
INNER JOIN dbo.courses c ON sc.course_id = c.course_id
WHERE sc.student_id = @TargetStudentId
ORDER BY c.course_name;

DECLARE @CrsId UNIQUEIDENTIFIER;
DECLARE @CrsName NVARCHAR(255);
DECLARE @GradeScore DECIMAL(5,2);
DECLARE @CourseCounter INT = 1;
DECLARE @TotalCourses INT = (SELECT COUNT(*) FROM @CourseIds);

DECLARE course_cursor CURSOR FOR SELECT course_id, course_name FROM @CourseIds;
OPEN course_cursor;
FETCH NEXT FROM course_cursor INTO @CrsId, @CrsName;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Vary the grades to show different performance levels across courses
    SET @GradeScore = CASE (@CourseCounter % 5)
        WHEN 0 THEN 94.50  -- Excellent
        WHEN 1 THEN 91.25  -- Very Good
        WHEN 2 THEN 87.75  -- Good
        WHEN 3 THEN 89.50  -- Very Good
        ELSE 92.00         -- Excellent
    END;

    -- Delete existing grade if present
    DELETE FROM dbo.student_course_grades 
    WHERE student_id = @TargetStudentId AND course_id = @CrsId;

    -- Insert new grade with realistic component scores
    INSERT INTO dbo.student_course_grades (grade_id, course_id, student_id, assignments_score, activities_score, exams_score, projects_score, updated_at)
    VALUES (
        NEWID(),
        @CrsId,
        @TargetStudentId,
        @GradeScore - 3,      -- Assignments slightly lower
        @GradeScore - 2,      -- Activities slightly lower
        @GradeScore + 2,      -- Exams slightly higher
        @GradeScore,          -- Projects match overall
        DATEADD(DAY, -(@CourseCounter * 2), @Now)
    );

    SET @CourseCounter += 1;
    FETCH NEXT FROM course_cursor INTO @CrsId, @CrsName;
END

CLOSE course_cursor;
DEALLOCATE course_cursor;

-- Add Assignment Submissions for Student 03324 (All assignments across all enrolled courses)
DECLARE @AssignmentIds TABLE (assignment_id UNIQUEIDENTIFIER, course_id UNIQUEIDENTIFIER, title NVARCHAR(255));
INSERT INTO @AssignmentIds
SELECT a.assignment_id, a.course_id, a.title
FROM dbo.class_assignments a
WHERE a.course_id IN (SELECT course_id FROM @CourseIds)
ORDER BY a.course_id, a.created_at;

DECLARE @AsgId UNIQUEIDENTIFIER;
DECLARE @AsgCourseId UNIQUEIDENTIFIER;
DECLARE @AsgTitle NVARCHAR(255);
DECLARE @SubmissionScore INT;
DECLARE @AssignmentCounter INT = 1;
DECLARE @TotalAssignments INT = (SELECT COUNT(*) FROM @AssignmentIds);

DECLARE assignment_cursor CURSOR FOR SELECT assignment_id, course_id, title FROM @AssignmentIds;
OPEN assignment_cursor;
FETCH NEXT FROM assignment_cursor INTO @AsgId, @AsgCourseId, @AsgTitle;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Vary submission scores realistically (85-98 range for good student)
    SET @SubmissionScore = CASE (@AssignmentCounter % 6)
        WHEN 0 THEN 96  -- Excellent
        WHEN 1 THEN 92  -- Very Good
        WHEN 2 THEN 88  -- Good
        WHEN 3 THEN 94  -- Excellent
        WHEN 4 THEN 90  -- Very Good
        ELSE 85         -- Good
    END;

    -- Check if submission already exists
    IF NOT EXISTS (SELECT 1 FROM dbo.assignment_submissions WHERE assignment_id = @AsgId AND student_id = @TargetStudentId)
    BEGIN
        INSERT INTO dbo.assignment_submissions (submission_id, assignment_id, student_id, submitted_at, score, status, notes, submission_content)
        VALUES (
            NEWID(),
            @AsgId,
            @TargetStudentId,
            DATEADD(DAY, -(@AssignmentCounter), @Now),
            @SubmissionScore,
            'graded',
            CASE (@AssignmentCounter % 4)
                WHEN 0 THEN 'Excellent work! Well-structured, thorough analysis, and excellent implementation.'
                WHEN 1 THEN 'Very good submission. Consider adding more detail to the methodology section for completeness.'
                WHEN 2 THEN 'Good work. Minor improvements needed in the conclusion and recommendations.'
                ELSE 'Solid submission. Well-executed with clear documentation and good attention to detail.'
            END,
            CONCAT('Submission for ', @AsgTitle, ': Comprehensive analysis with detailed implementation, clear documentation, and thoughtful recommendations based on research.')
        );
    END

    SET @AssignmentCounter += 1;
    FETCH NEXT FROM assignment_cursor INTO @AsgId, @AsgCourseId, @AsgTitle;
END

CLOSE assignment_cursor;
DEALLOCATE assignment_cursor;

    PRINT 'Seeding completed for student03324@university.edu with achievements, conversations, tickets, course grades, and assignments.';
END
