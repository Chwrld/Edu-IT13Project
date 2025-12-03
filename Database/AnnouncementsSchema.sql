USE EduCRM;
GO

-- Create announcements table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'announcements')
BEGIN
    CREATE TABLE announcements (
        announcement_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        title NVARCHAR(255) NOT NULL,
        content NVARCHAR(MAX) NOT NULL,
        author_id UNIQUEIDENTIFIER NOT NULL,
        visibility NVARCHAR(50) NOT NULL DEFAULT 'all', -- all, students, advisers
        is_published BIT NOT NULL DEFAULT 1,
        created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        created_by UNIQUEIDENTIFIER NOT NULL,
        updated_at DATETIME2,
        updated_by UNIQUEIDENTIFIER,
        FOREIGN KEY (author_id) REFERENCES users(user_id),
        FOREIGN KEY (created_by) REFERENCES users(user_id),
        FOREIGN KEY (updated_by) REFERENCES users(user_id)
    );

    CREATE INDEX idx_announcements_visibility ON announcements(visibility);
    CREATE INDEX idx_announcements_created_at ON announcements(created_at DESC);
END
GO

-- Create announcement_views table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'announcement_views')
BEGIN
    CREATE TABLE announcement_views (
        view_id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        announcement_id UNIQUEIDENTIFIER NOT NULL,
        user_id UNIQUEIDENTIFIER NOT NULL,
        viewed_at DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY (announcement_id) REFERENCES announcements(announcement_id),
        FOREIGN KEY (user_id) REFERENCES users(user_id)
    );

    CREATE INDEX idx_announcement_views_announcement ON announcement_views(announcement_id);
    CREATE INDEX idx_announcement_views_user ON announcement_views(user_id);
END
GO
