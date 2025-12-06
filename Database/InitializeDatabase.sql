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
    profile_picture_url NVARCHAR(MAX),
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE()
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
    visibility NVARCHAR(50) NOT NULL DEFAULT 'all',
    is_published BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    created_by UNIQUEIDENTIFIER NOT NULL,
    updated_at DATETIME2,
    updated_by UNIQUEIDENTIFIER,
    FOREIGN KEY (author_id) REFERENCES dbo.users(user_id),
    FOREIGN KEY (created_by) REFERENCES dbo.users(user_id),
    FOREIGN KEY (updated_by) REFERENCES dbo.users(user_id)
);
GO

CREATE INDEX idx_announcements_visibility ON dbo.announcements(visibility);
CREATE INDEX idx_announcements_created ON dbo.announcements(created_at DESC);
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
-- Deterministic System Accounts
------------------------------------------------------------

DECLARE @Now DATETIME2 = GETUTCDATE();
DECLARE @AdminId UNIQUEIDENTIFIER = 'F5E0A1E7-7C77-4A04-9E68-7B7F7A7D0001';
DECLARE @TeacherPrimaryId UNIQUEIDENTIFIER = 'F5E0A1E7-7C77-4A04-9E68-7B7F7A7D0002';
DECLARE @TeacherSecondaryId UNIQUEIDENTIFIER = 'F5E0A1E7-7C77-4A04-9E68-7B7F7A7D0003';
DECLARE @StudentPrimaryId UNIQUEIDENTIFIER = 'F5E0A1E7-7C77-4A04-9E68-7B7F7A7D0004';

INSERT INTO dbo.users (user_id, email, password_hash, password_salt, role, display_name, phone_number, address, profile_picture_url, is_active, created_at)
VALUES
    (@AdminId, 'admin@university.edu', '+9/zNWABQWmUz7vYKwkG0wPfoZCxZ9g2OnnM6r1EvqI=', 'gvVIA8tmIqly64N8Zqt1zg==', 'Admin', 'Admin User', '+1 (555) 345-6789', 'Administration Building, Room 100', NULL, 1, @Now),
    (@TeacherPrimaryId, 'teacher@university.edu', 'YM5TCtG/TtHZMmqP2YllL1IRlBw4m8ny01ESx3atldQ=', 'pMoH0ATf/TwF8GKJetrbyA==', 'Teacher', 'John Teacher', '+1 (555) 234-5678', 'Faculty Building, Office 301', NULL, 1, @Now),
    (@TeacherSecondaryId, 'dr.smith@university.edu', 'YM5TCtG/TtHZMmqP2YllL1IRlBw4m8ny01ESx3atldQ=', 'pMoH0ATf/TwF8GKJetrbyA==', 'Teacher', 'Dr. Smith', '+1 (555) 456-7890', 'Faculty Building, Office 302', NULL, 1, @Now),
    (@StudentPrimaryId, 'student@university.edu', '2Dyfiau1pDwzyQxBQCpHWri9i7pmcQVJBLtnLW/g+ag=', 'sYCDlcmAoN56njDjs9uTag==', 'Student', 'Sarah Student', '+1 (555) 123-4567', 'Building A, Room 205, Campus', NULL, 1, @Now);

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

    INSERT INTO dbo.users (user_id, email, password_hash, password_salt, role, display_name, phone_number, address, profile_picture_url, is_active, created_at)
    VALUES (
        @TeacherUserId,
        @TeacherEmail,
        'YM5TCtG/TtHZMmqP2YllL1IRlBw4m8ny01ESx3atldQ=',
        'pMoH0ATf/TwF8GKJetrbyA==',
        'Teacher',
        @TeacherDisplayName,
        CONCAT('+1 (555) ', FORMAT(2300 + @TeacherIndex, '000'), '-890'),
        CONCAT('Faculty Building, Office ', FORMAT(200 + @TeacherIndex, '000')),
        NULL,
        1,
        @Now
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

        DECLARE @StudentIndex INT = 1;

        WHILE @StudentIndex <= @StudentsPerClass
        BEGIN
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

            INSERT INTO dbo.users (user_id, email, password_hash, password_salt, role, display_name, phone_number, address, profile_picture_url, is_active, created_at)
            VALUES (
                @StudentUserId,
                @StudentEmail,
                '2Dyfiau1pDwzyQxBQCpHWri9i7pmcQVJBLtnLW/g+ag=',
                'sYCDlcmAoN56njDjs9uTag==',
                'Student',
                @StudentName,
                CONCAT('+1 (555) ', FORMAT(3100 + @StudentIndex, '000'), '-', FORMAT(7000 + @ClassIndex, '0000')),
                CONCAT('Dorm ', CHAR(64 + @ClassIndex), ', Room ', FORMAT(@StudentIndex, '000')),
                NULL,
                1,
                @Now
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

    SET @TeacherIndex += 1;
END

PRINT 'EduCRM database created and fully seeded.';
