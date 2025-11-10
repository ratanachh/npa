using System.Data;
using Dapper;

namespace UdemyCloneSaaS.Api.Data;

/// <summary>
/// Initializes the database schema on application startup.
/// </summary>
public class DatabaseInitializer
{
    private readonly IDbConnection _connection;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IDbConnection connection, ILogger<DatabaseInitializer> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    /// <summary>
    /// Creates all required tables if they don't exist.
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing database schema...");

        try
        {
            // Create students table
            await _connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS students (
                    id BIGSERIAL PRIMARY KEY,
                    tenant_id VARCHAR(100) NOT NULL,
                    email VARCHAR(255) NOT NULL,
                    name VARCHAR(255) NOT NULL,
                    avatar_url TEXT,
                    enrolled_courses_count INTEGER DEFAULT 0,
                    completed_courses_count INTEGER DEFAULT 0,
                    total_learning_hours DECIMAL(10, 2) DEFAULT 0.0,
                    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                    last_active_at TIMESTAMP,
                    UNIQUE(tenant_id, email)
                );");

            // Create courses table
            await _connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS courses (
                    id BIGSERIAL PRIMARY KEY,
                    tenant_id VARCHAR(100) NOT NULL,
                    instructor_id BIGINT NOT NULL,
                    category_id BIGINT NOT NULL,
                    title VARCHAR(500) NOT NULL,
                    slug VARCHAR(500) NOT NULL,
                    description TEXT,
                    short_description TEXT,
                    thumbnail_url TEXT,
                    video_preview_url TEXT,
                    price DECIMAL(10, 2) DEFAULT 0.0,
                    discount_price DECIMAL(10, 2),
                    level VARCHAR(50) DEFAULT 'Beginner',
                    language VARCHAR(50) DEFAULT 'English',
                    duration_hours DECIMAL(10, 2) DEFAULT 0.0,
                    total_lessons INTEGER DEFAULT 0,
                    enrolled_students_count INTEGER DEFAULT 0,
                    average_rating DECIMAL(3, 2) DEFAULT 0.0,
                    total_reviews INTEGER DEFAULT 0,
                    status VARCHAR(50) DEFAULT 'Draft',
                    is_featured BOOLEAN DEFAULT FALSE,
                    published_at TIMESTAMP,
                    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
                    UNIQUE(tenant_id, slug)
                );");

            // Create enrollments table
            await _connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS enrollments (
                    id BIGSERIAL PRIMARY KEY,
                    tenant_id VARCHAR(100) NOT NULL,
                    student_id BIGINT NOT NULL,
                    course_id BIGINT NOT NULL,
                    enrolled_at TIMESTAMP NOT NULL DEFAULT NOW(),
                    progress_percentage DECIMAL(5, 2) DEFAULT 0.0,
                    completed_lessons_count INTEGER DEFAULT 0,
                    is_completed BOOLEAN DEFAULT FALSE,
                    completed_at TIMESTAMP,
                    last_accessed_at TIMESTAMP,
                    total_watch_time_minutes INTEGER DEFAULT 0,
                    certificate_issued BOOLEAN DEFAULT FALSE,
                    UNIQUE(tenant_id, student_id, course_id)
                );");

            // Create indexes
            await _connection.ExecuteAsync(@"
                CREATE INDEX IF NOT EXISTS idx_students_tenant ON students(tenant_id);
                CREATE INDEX IF NOT EXISTS idx_students_email ON students(email);
                
                CREATE INDEX IF NOT EXISTS idx_courses_tenant ON courses(tenant_id);
                CREATE INDEX IF NOT EXISTS idx_courses_instructor ON courses(instructor_id);
                CREATE INDEX IF NOT EXISTS idx_courses_category ON courses(category_id);
                CREATE INDEX IF NOT EXISTS idx_courses_status ON courses(status);
                CREATE INDEX IF NOT EXISTS idx_courses_featured ON courses(is_featured);
                
                CREATE INDEX IF NOT EXISTS idx_enrollments_tenant ON enrollments(tenant_id);
                CREATE INDEX IF NOT EXISTS idx_enrollments_student ON enrollments(student_id);
                CREATE INDEX IF NOT EXISTS idx_enrollments_course ON enrollments(course_id);
                CREATE INDEX IF NOT EXISTS idx_enrollments_completed ON enrollments(is_completed);
            ");

            _logger.LogInformation("Database schema initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing database schema");
            throw;
        }
    }

    /// <summary>
    /// Seeds initial sample data for testing.
    /// </summary>
    public async Task SeedDataAsync()
    {
        _logger.LogInformation("Checking if sample data needs to be seeded...");

        // Check if data already exists
        var studentCount = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM students");
        if (studentCount > 0)
        {
            _logger.LogInformation("Sample data already exists, skipping seed");
            return;
        }

        _logger.LogInformation("Seeding sample data...");

        try
        {
            // Insert sample students
            await _connection.ExecuteAsync(@"
                INSERT INTO students (tenant_id, email, name, avatar_url, enrolled_courses_count, total_learning_hours)
                VALUES 
                    ('demo-tenant', 'john.doe@example.com', 'John Doe', 'https://i.pravatar.cc/150?img=1', 3, 12.5),
                    ('demo-tenant', 'jane.smith@example.com', 'Jane Smith', 'https://i.pravatar.cc/150?img=2', 5, 24.0),
                    ('demo-tenant', 'bob.wilson@example.com', 'Bob Wilson', 'https://i.pravatar.cc/150?img=3', 2, 8.0);
            ");

            // Insert sample courses
            await _connection.ExecuteAsync(@"
                INSERT INTO courses (tenant_id, instructor_id, category_id, title, slug, description, price, level, duration_hours, total_lessons, enrolled_students_count, average_rating, total_reviews, status, is_featured, published_at)
                VALUES 
                    ('demo-tenant', 1, 1, 'Complete C# Masterclass', 'complete-csharp-masterclass', 'Learn C# from beginner to advanced', 49.99, 'Beginner', 20.5, 150, 1250, 4.7, 456, 'Published', true, NOW()),
                    ('demo-tenant', 1, 1, 'ASP.NET Core Web API Development', 'aspnet-core-web-api', 'Build modern RESTful APIs with ASP.NET Core', 59.99, 'Intermediate', 15.0, 120, 890, 4.8, 320, 'Published', true, NOW()),
                    ('demo-tenant', 2, 2, 'React - The Complete Guide', 'react-complete-guide', 'Master React with hooks, context, and more', 54.99, 'Intermediate', 25.0, 180, 2100, 4.9, 780, 'Published', false, NOW()),
                    ('demo-tenant', 2, 3, 'Python for Data Science', 'python-data-science', 'Data analysis and visualization with Python', 44.99, 'Beginner', 18.0, 140, 1560, 4.6, 420, 'Published', false, NOW()),
                    ('demo-tenant', 3, 4, 'Docker and Kubernetes Mastery', 'docker-kubernetes-mastery', 'Container orchestration made easy', 69.99, 'Advanced', 22.0, 160, 670, 4.8, 210, 'Published', true, NOW());
            ");

            // Insert sample enrollments
            await _connection.ExecuteAsync(@"
                INSERT INTO enrollments (tenant_id, student_id, course_id, progress_percentage, completed_lessons_count, is_completed, last_accessed_at, total_watch_time_minutes)
                VALUES 
                    ('demo-tenant', 1, 1, 75.0, 112, false, NOW() - INTERVAL '2 hours', 450),
                    ('demo-tenant', 1, 2, 100.0, 120, true, NOW() - INTERVAL '1 day', 540),
                    ('demo-tenant', 1, 3, 30.0, 54, false, NOW() - INTERVAL '5 hours', 180),
                    ('demo-tenant', 2, 1, 100.0, 150, true, NOW() - INTERVAL '3 days', 615),
                    ('demo-tenant', 2, 3, 80.0, 144, false, NOW() - INTERVAL '1 hour', 720),
                    ('demo-tenant', 3, 2, 45.0, 54, false, NOW() - INTERVAL '12 hours', 240);
            ");

            _logger.LogInformation("Sample data seeded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding sample data");
            throw;
        }
    }
}
