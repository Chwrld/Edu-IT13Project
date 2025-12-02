-- Create EduCRM database if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'EduCRM')
BEGIN
    CREATE DATABASE EduCRM;
END
GO

USE EduCRM;
GO

-- Create users table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'users')
BEGIN
    CREATE TABLE users (
        user_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
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
    
    CREATE INDEX idx_email ON users(email);
END
GO

-- Create students table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'students')
BEGIN
    CREATE TABLE students (
        student_id UNIQUEIDENTIFIER PRIMARY KEY,
        student_number NVARCHAR(50) NOT NULL UNIQUE,
        program NVARCHAR(255),
        year_level NVARCHAR(50),
        gpa DECIMAL(3,2),
        status NVARCHAR(50) NOT NULL DEFAULT 'active',
        adviser_id UNIQUEIDENTIFIER,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY (student_id) REFERENCES users(user_id),
        FOREIGN KEY (adviser_id) REFERENCES users(user_id)
    );
    
    CREATE INDEX idx_student_number ON students(student_number);
    CREATE INDEX idx_adviser ON students(adviser_id);
END
GO

-- Create student_achievements table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'student_achievements')
BEGIN
    CREATE TABLE student_achievements (
        achievement_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        student_id UNIQUEIDENTIFIER NOT NULL,
        achievement_name NVARCHAR(255) NOT NULL,
        description NVARCHAR(MAX),
        awarded_by UNIQUEIDENTIFIER,
        awarded_date DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY (student_id) REFERENCES students(student_id),
        FOREIGN KEY (awarded_by) REFERENCES users(user_id)
    );
    
    CREATE INDEX idx_student_achievements ON student_achievements(student_id);
    CREATE INDEX idx_awarded_by ON student_achievements(awarded_by);
END
GO

-- Create messages table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'messages')
BEGIN
    CREATE TABLE messages (
        message_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        sender_id UNIQUEIDENTIFIER NOT NULL,
        receiver_id UNIQUEIDENTIFIER NOT NULL,
        content NVARCHAR(MAX) NOT NULL,
        is_read BIT NOT NULL DEFAULT 0,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY (sender_id) REFERENCES users(user_id),
        FOREIGN KEY (receiver_id) REFERENCES users(user_id)
    );
    
    CREATE INDEX idx_sender ON messages(sender_id);
    CREATE INDEX idx_receiver ON messages(receiver_id);
    CREATE INDEX idx_created ON messages(created_at);
END
GO

-- Create conversations table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'conversations')
BEGIN
    CREATE TABLE conversations (
        conversation_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        participant1_id UNIQUEIDENTIFIER NOT NULL,
        participant2_id UNIQUEIDENTIFIER NOT NULL,
        last_message_id UNIQUEIDENTIFIER,
        last_message_time DATETIME2,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY (participant1_id) REFERENCES users(user_id),
        FOREIGN KEY (participant2_id) REFERENCES users(user_id),
        FOREIGN KEY (last_message_id) REFERENCES messages(message_id)
    );
    
    CREATE INDEX idx_participant1 ON conversations(participant1_id);
    CREATE INDEX idx_participant2 ON conversations(participant2_id);
END
GO

-- Create support_tickets table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'support_tickets')
BEGIN
    CREATE TABLE support_tickets (
        ticket_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
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
        FOREIGN KEY (created_by) REFERENCES users(user_id),
        FOREIGN KEY (updated_by) REFERENCES users(user_id),
        FOREIGN KEY (student_id) REFERENCES users(user_id),
        FOREIGN KEY (assigned_to_id) REFERENCES users(user_id)
    );
    
    CREATE INDEX idx_student ON support_tickets(student_id);
    CREATE INDEX idx_status ON support_tickets(status);
    CREATE INDEX idx_created ON support_tickets(created_at);
END
GO

-- Create ticket_comments table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ticket_comments')
BEGIN
    CREATE TABLE ticket_comments (
        comment_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        ticket_id UNIQUEIDENTIFIER NOT NULL,
        user_id UNIQUEIDENTIFIER NOT NULL,
        content NVARCHAR(MAX) NOT NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY (ticket_id) REFERENCES support_tickets(ticket_id),
        FOREIGN KEY (user_id) REFERENCES users(user_id)
    );
    
    CREATE INDEX idx_ticket ON ticket_comments(ticket_id);
    CREATE INDEX idx_user ON ticket_comments(user_id);
END
GO

-- Declare variables for seeding
DECLARE @StudentId UNIQUEIDENTIFIER;
DECLARE @TeacherId UNIQUEIDENTIFIER;
DECLARE @Teacher2Id UNIQUEIDENTIFIER;
DECLARE @AdminId UNIQUEIDENTIFIER;

-- Seed users if they don't exist
IF NOT EXISTS (SELECT 1 FROM users WHERE email = 'student@university.edu')
BEGIN
    INSERT INTO users (user_id, email, password_hash, password_salt, role, display_name, phone_number, address, profile_picture_url, is_active, created_at)
    VALUES 
        (NEWID(), 'student@university.edu', '2Dyfiau1pDwzyQxBQCpHWri9i7pmcQVJBLtnLW/g+ag=', 'sYCDlcmAoN56njDjs9uTag==', 'Student', 'Sarah Student', '+1 (555) 123-4567', 'Building A, Room 205, Campus', NULL, 1, GETUTCDATE()),
        (NEWID(), 'teacher@university.edu', 'YM5TCtG/TtHZMmqP2YllL1IRlBw4m8ny01ESx3atldQ=', 'pMoH0ATf/TwF8GKJetrbyA==', 'Teacher', 'John Teacher', '+1 (555) 234-5678', 'Faculty Building, Office 301', NULL, 1, GETUTCDATE()),
        (NEWID(), 'dr.smith@university.edu', 'YM5TCtG/TtHZMmqP2YllL1IRlBw4m8ny01ESx3atldQ=', 'pMoH0ATf/TwF8GKJetrbyA==', 'Teacher', 'Dr. Smith', '+1 (555) 456-7890', 'Faculty Building, Office 302', NULL, 1, GETUTCDATE()),
        (NEWID(), 'admin@university.edu', '+9/zNWABQWmUz7vYKwkG0wPfoZCxZ9g2OnnM6r1EvqI=', 'gvVIA8tmIqly64N8Zqt1zg==', 'Admin', 'Admin User', '+1 (555) 345-6789', 'Administration Building, Room 100', NULL, 1, GETUTCDATE());
END

-- Get user IDs for seeding
SELECT @StudentId = user_id FROM users WHERE email = 'student@university.edu';
SELECT @TeacherId = user_id FROM users WHERE email = 'teacher@university.edu';
SELECT @Teacher2Id = user_id FROM users WHERE email = 'dr.smith@university.edu';
SELECT @AdminId = user_id FROM users WHERE email = 'admin@university.edu';

-- Insert student record if not exists
IF @StudentId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM students WHERE student_id = @StudentId)
BEGIN
    INSERT INTO students (student_id, student_number, program, year_level, gpa, status, adviser_id, created_at)
    VALUES (@StudentId, 'STU-2024-0001', 'Bachelor of Science in Computer Science', 'Year 3', 3.85, 'active', @TeacherId, GETUTCDATE());
END

-- Seed student achievements if not exists
IF @StudentId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM student_achievements WHERE student_id = @StudentId)
BEGIN
    INSERT INTO student_achievements (achievement_id, student_id, achievement_name, description, awarded_by, awarded_date)
    VALUES
        (NEWID(), @StudentId, 'Dean''s List', 'Achieved Dean''s List recognition for academic excellence in Fall 2024', @TeacherId, DATEADD(MONTH, -2, GETUTCDATE())),
        (NEWID(), @StudentId, 'Research Grant', 'Awarded research grant for innovative AI project proposal', @TeacherId, DATEADD(MONTH, -3, GETUTCDATE())),
        (NEWID(), @StudentId, 'Hackathon Winner', 'First place winner in University Hackathon 2024', @AdminId, DATEADD(MONTH, -1, GETUTCDATE()));
END

-- Seed sample messages and conversations if users exist
IF @StudentId IS NOT NULL AND @TeacherId IS NOT NULL
BEGIN
    -- Delete existing messages and conversations to start fresh
    DELETE FROM conversations;
    DELETE FROM messages;
    
    DECLARE @LastMessageId UNIQUEIDENTIFIER;
    DECLARE @Msg1Id UNIQUEIDENTIFIER = NEWID();
    DECLARE @Msg2Id UNIQUEIDENTIFIER = NEWID();
    DECLARE @Msg3Id UNIQUEIDENTIFIER = NEWID();
    DECLARE @Msg4Id UNIQUEIDENTIFIER = NEWID();
    DECLARE @Msg5Id UNIQUEIDENTIFIER = NEWID();
    DECLARE @Msg6Id UNIQUEIDENTIFIER = NEWID();
    
    -- Insert sample messages between student and teacher (John Teacher)
    INSERT INTO messages (message_id, sender_id, receiver_id, content, is_read, created_at)
    VALUES
        (@Msg1Id, @TeacherId, @StudentId, 'Hi Sarah, I''ve reviewed your thesis draft.', 0, DATEADD(MINUTE, -30, GETUTCDATE())),
        (@Msg2Id, @TeacherId, @StudentId, 'Overall it''s excellent work, just a few minor suggestions on the methodology section.', 0, DATEADD(MINUTE, -28, GETUTCDATE())),
        (@Msg3Id, @StudentId, @TeacherId, 'Thank you! I''ll love to hear your feedback.', 1, DATEADD(MINUTE, -20, GETUTCDATE())),
        (@Msg4Id, @StudentId, @TeacherId, 'Absolutely! Would Thursday afternoon work for you?', 1, DATEADD(MINUTE, -15, GETUTCDATE())),
        (@Msg5Id, @Teacher2Id, @StudentId, 'Hello Sarah, welcome to my office hours!', 0, DATEADD(MINUTE, -45, GETUTCDATE())),
        (@Msg6Id, @StudentId, @Teacher2Id, 'Thank you Dr. Smith, I look forward to discussing my project.', 1, DATEADD(MINUTE, -40, GETUTCDATE()));
    
    -- Insert conversation between student and teacher (John Teacher) with last message reference
    INSERT INTO conversations (conversation_id, participant1_id, participant2_id, last_message_id, last_message_time, created_at)
    VALUES
        (NEWID(), @StudentId, @TeacherId, @Msg4Id, DATEADD(MINUTE, -15, GETUTCDATE()), DATEADD(DAY, -5, GETUTCDATE())),
        (NEWID(), @StudentId, @Teacher2Id, @Msg6Id, DATEADD(MINUTE, -40, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()));
END
GO
