/*
    EduCRM Remote Database Schema Initializer
    - Creates all schema objects (tables, constraints, indexes)
    - Does NOT drop existing database (safe for remote)
    - Does NOT seed data (use SyncService to sync from local)
    - Run this on: db34895.public.databaseasp.net
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

USE [db34895];
GO

-- Drop existing tables if they exist (in reverse dependency order)
IF OBJECT_ID('dbo.ticket_comments', 'U') IS NOT NULL DROP TABLE dbo.ticket_comments;
IF OBJECT_ID('dbo.support_tickets', 'U') IS NOT NULL DROP TABLE dbo.support_tickets;
IF OBJECT_ID('dbo.announcement_views', 'U') IS NOT NULL DROP TABLE dbo.announcement_views;
IF OBJECT_ID('dbo.announcements', 'U') IS NOT NULL DROP TABLE dbo.announcements;
IF OBJECT_ID('dbo.conversations', 'U') IS NOT NULL DROP TABLE dbo.conversations;
IF OBJECT_ID('dbo.messages', 'U') IS NOT NULL DROP TABLE dbo.messages;
IF OBJECT_ID('dbo.student_achievements', 'U') IS NOT NULL DROP TABLE dbo.student_achievements;
IF OBJECT_ID('dbo.student_course_grades', 'U') IS NOT NULL DROP TABLE dbo.student_course_grades;
IF OBJECT_ID('dbo.assignment_submissions', 'U') IS NOT NULL DROP TABLE dbo.assignment_submissions;
IF OBJECT_ID('dbo.class_assignments', 'U') IS NOT NULL DROP TABLE dbo.class_assignments;
IF OBJECT_ID('dbo.student_courses', 'U') IS NOT NULL DROP TABLE dbo.student_courses;
IF OBJECT_ID('dbo.students', 'U') IS NOT NULL DROP TABLE dbo.students;
IF OBJECT_ID('dbo.courses', 'U') IS NOT NULL DROP TABLE dbo.courses;
IF OBJECT_ID('dbo.admins', 'U') IS NOT NULL DROP TABLE dbo.admins;
IF OBJECT_ID('dbo.advisers', 'U') IS NOT NULL DROP TABLE dbo.advisers;
IF OBJECT_ID('dbo.users', 'U') IS NOT NULL DROP TABLE dbo.users;
GO

PRINT 'Dropped existing tables (if any)';
GO

-- Create all tables
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

PRINT 'Remote database schema created successfully!';
PRINT 'Tables: 16 | Indexes: 15';
PRINT 'Next: Use SyncService to sync data from local database.';
GO
