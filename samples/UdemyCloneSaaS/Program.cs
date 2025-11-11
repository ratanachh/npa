using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.Core;
using NPA.Extensions.MultiTenancy;
using NPA.Providers.PostgreSql.Extensions;
using Npgsql;
using Testcontainers.PostgreSql;
using UdemyCloneSaaS.Entities;
using UdemyCloneSaaS.Repositories;
using UdemyCloneSaaS.Services;

namespace UdemyCloneSaaS;

/// <summary>
/// Udemy Clone SaaS - Real-world multi-tenant learning platform sample.
/// 
/// Demonstrates:
/// - Multi-tenant architecture (each organization has isolated data)
/// - Complex domain model (Courses, Instructors, Students, Enrollments, Reviews, Payments)
/// - Repository pattern with source generators
/// - Business logic services
/// - Payment processing
/// - Review and rating system
/// - Course progress tracking
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║          Udemy Clone SaaS - Multi-Tenant Learning Platform    ║");
        Console.WriteLine("║                  Built with NPA Framework                      ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

        // Setup PostgreSQL testcontainer
        var postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("udemy_clone_saas")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithCleanUp(true)
            .Build();

        await using (postgresContainer)
        {
            Console.WriteLine("Starting PostgreSQL container...");
            await postgresContainer.StartAsync();
            Console.WriteLine("✓ PostgreSQL container started\n");

            var connectionString = postgresContainer.GetConnectionString();

            // Initialize database schema
            await InitializeDatabaseAsync(connectionString);

            // Setup dependency injection
            var services = new ServiceCollection();
            
            // NPA providers
            services.AddPostgreSqlProvider(connectionString);
            services.AddMultiTenancy();
            
            // Logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });

            // Register repositories (source generated implementations will be auto-registered)
            // Note: In a real app, repositories would be auto-registered by DI container
            
            // Register services
            services.AddScoped<CourseService>();
            services.AddScoped<EnrollmentService>();
            services.AddScoped<ReviewService>();
            services.AddScoped<PaymentService>();

            var serviceProvider = services.BuildServiceProvider();
            var entityManager = serviceProvider.GetRequiredService<IEntityManager>();
            var tenantManager = serviceProvider.GetRequiredService<TenantManager>();

            // Run demo scenarios
            await RunUdemyCloneDemoAsync(entityManager, tenantManager);

            Console.WriteLine("\n\n[Completed] Demo completed! Press any key to exit...");
            Console.ReadKey();
        }
    }

    static async Task InitializeDatabaseAsync(string connectionString)
    {
        Console.WriteLine("Initializing database schema...\n");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();

        // Create all tables
        var schemas = new[]
        {
            // Tenants
            @"CREATE TABLE IF NOT EXISTS tenants (
                id BIGSERIAL PRIMARY KEY,
                tenant_id VARCHAR(100) NOT NULL UNIQUE,
                name VARCHAR(255) NOT NULL,
                domain VARCHAR(255),
                subscription_tier VARCHAR(50) DEFAULT 'Free',
                max_instructors INTEGER DEFAULT 10,
                max_courses INTEGER DEFAULT 50,
                max_students INTEGER DEFAULT 1000,
                is_active BOOLEAN DEFAULT true,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                trial_ends_at TIMESTAMP
            );",
            
            // Categories
            @"CREATE TABLE IF NOT EXISTS categories (
                id BIGSERIAL PRIMARY KEY,
                tenant_id VARCHAR(100) NOT NULL,
                name VARCHAR(255) NOT NULL,
                slug VARCHAR(255) NOT NULL,
                description TEXT,
                icon VARCHAR(255),
                parent_category_id BIGINT REFERENCES categories(id),
                course_count INTEGER DEFAULT 0,
                is_active BOOLEAN DEFAULT true,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            CREATE INDEX IF NOT EXISTS idx_categories_tenant ON categories(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_categories_slug ON categories(tenant_id, slug);",
            
            // Instructors
            @"CREATE TABLE IF NOT EXISTS instructors (
                id BIGSERIAL PRIMARY KEY,
                tenant_id VARCHAR(100) NOT NULL,
                email VARCHAR(255) NOT NULL,
                name VARCHAR(255) NOT NULL,
                bio TEXT,
                title VARCHAR(255),
                avatar_url VARCHAR(500),
                total_students INTEGER DEFAULT 0,
                total_reviews INTEGER DEFAULT 0,
                average_rating DECIMAL(3,2) DEFAULT 0.0,
                is_verified BOOLEAN DEFAULT false,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            CREATE INDEX IF NOT EXISTS idx_instructors_tenant ON instructors(tenant_id);
            CREATE UNIQUE INDEX IF NOT EXISTS idx_instructors_email ON instructors(tenant_id, email);",
            
            // Students
            @"CREATE TABLE IF NOT EXISTS students (
                id BIGSERIAL PRIMARY KEY,
                tenant_id VARCHAR(100) NOT NULL,
                email VARCHAR(255) NOT NULL,
                name VARCHAR(255) NOT NULL,
                avatar_url VARCHAR(500),
                enrolled_courses_count INTEGER DEFAULT 0,
                completed_courses_count INTEGER DEFAULT 0,
                total_learning_hours DECIMAL(10,2) DEFAULT 0.0,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                last_active_at TIMESTAMP
            );
            CREATE INDEX IF NOT EXISTS idx_students_tenant ON students(tenant_id);
            CREATE UNIQUE INDEX IF NOT EXISTS idx_students_email ON students(tenant_id, email);",
            
            // Courses
            @"CREATE TABLE IF NOT EXISTS courses (
                id BIGSERIAL PRIMARY KEY,
                tenant_id VARCHAR(100) NOT NULL,
                instructor_id BIGINT NOT NULL REFERENCES instructors(id),
                category_id BIGINT NOT NULL REFERENCES categories(id),
                title VARCHAR(500) NOT NULL,
                slug VARCHAR(500) NOT NULL,
                description TEXT,
                short_description VARCHAR(500),
                thumbnail_url VARCHAR(500),
                video_preview_url VARCHAR(500),
                price DECIMAL(10,2) DEFAULT 0.0,
                discount_price DECIMAL(10,2),
                level VARCHAR(50) DEFAULT 'Beginner',
                language VARCHAR(50) DEFAULT 'English',
                duration_hours DECIMAL(10,2) DEFAULT 0.0,
                total_lessons INTEGER DEFAULT 0,
                enrolled_students_count INTEGER DEFAULT 0,
                average_rating DECIMAL(3,2) DEFAULT 0.0,
                total_reviews INTEGER DEFAULT 0,
                status VARCHAR(50) DEFAULT 'Draft',
                is_featured BOOLEAN DEFAULT false,
                published_at TIMESTAMP,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            CREATE INDEX IF NOT EXISTS idx_courses_tenant ON courses(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_courses_instructor ON courses(instructor_id);
            CREATE INDEX IF NOT EXISTS idx_courses_category ON courses(category_id);
            CREATE INDEX IF NOT EXISTS idx_courses_status ON courses(tenant_id, status);
            CREATE INDEX IF NOT EXISTS idx_courses_slug ON courses(tenant_id, slug);",
            
            // Lessons
            @"CREATE TABLE IF NOT EXISTS lessons (
                id BIGSERIAL PRIMARY KEY,
                tenant_id VARCHAR(100) NOT NULL,
                course_id BIGINT NOT NULL REFERENCES courses(id) ON DELETE CASCADE,
                title VARCHAR(500) NOT NULL,
                description TEXT,
                video_url VARCHAR(500),
                duration_minutes INTEGER DEFAULT 0,
                order_index INTEGER DEFAULT 0,
                is_free_preview BOOLEAN DEFAULT false,
                content_type VARCHAR(50) DEFAULT 'Video',
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            CREATE INDEX IF NOT EXISTS idx_lessons_tenant ON lessons(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_lessons_course ON lessons(course_id);",
            
            // Enrollments
            @"CREATE TABLE IF NOT EXISTS enrollments (
                id BIGSERIAL PRIMARY KEY,
                tenant_id VARCHAR(100) NOT NULL,
                student_id BIGINT NOT NULL REFERENCES students(id),
                course_id BIGINT NOT NULL REFERENCES courses(id),
                enrolled_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                progress_percentage DECIMAL(5,2) DEFAULT 0.0,
                completed_lessons_count INTEGER DEFAULT 0,
                is_completed BOOLEAN DEFAULT false,
                completed_at TIMESTAMP,
                last_accessed_at TIMESTAMP,
                total_watch_time_minutes INTEGER DEFAULT 0,
                certificate_issued BOOLEAN DEFAULT false
            );
            CREATE INDEX IF NOT EXISTS idx_enrollments_tenant ON enrollments(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_enrollments_student ON enrollments(student_id);
            CREATE INDEX IF NOT EXISTS idx_enrollments_course ON enrollments(course_id);
            CREATE UNIQUE INDEX IF NOT EXISTS idx_enrollments_student_course ON enrollments(student_id, course_id);",
            
            // Reviews
            @"CREATE TABLE IF NOT EXISTS reviews (
                id BIGSERIAL PRIMARY KEY,
                tenant_id VARCHAR(100) NOT NULL,
                course_id BIGINT NOT NULL REFERENCES courses(id),
                student_id BIGINT NOT NULL REFERENCES students(id),
                rating INTEGER NOT NULL CHECK (rating >= 1 AND rating <= 5),
                title VARCHAR(255),
                comment TEXT,
                is_verified_purchase BOOLEAN DEFAULT true,
                helpful_count INTEGER DEFAULT 0,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP
            );
            CREATE INDEX IF NOT EXISTS idx_reviews_tenant ON reviews(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_reviews_course ON reviews(course_id);
            CREATE INDEX IF NOT EXISTS idx_reviews_student ON reviews(student_id);
            CREATE UNIQUE INDEX IF NOT EXISTS idx_reviews_student_course ON reviews(student_id, course_id);",
            
            // Payments
            @"CREATE TABLE IF NOT EXISTS payments (
                id BIGSERIAL PRIMARY KEY,
                tenant_id VARCHAR(100) NOT NULL,
                student_id BIGINT NOT NULL REFERENCES students(id),
                course_id BIGINT NOT NULL REFERENCES courses(id),
                enrollment_id BIGINT REFERENCES enrollments(id),
                amount DECIMAL(10,2) NOT NULL,
                currency VARCHAR(3) DEFAULT 'USD',
                payment_method VARCHAR(50) DEFAULT 'CreditCard',
                transaction_id VARCHAR(255),
                status VARCHAR(50) DEFAULT 'Pending',
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                completed_at TIMESTAMP
            );
            CREATE INDEX IF NOT EXISTS idx_payments_tenant ON payments(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_payments_student ON payments(student_id);
            CREATE INDEX IF NOT EXISTS idx_payments_course ON payments(course_id);
            CREATE INDEX IF NOT EXISTS idx_payments_status ON payments(status);"
        };

        foreach (var schema in schemas)
        {
            command.CommandText = schema;
            await command.ExecuteNonQueryAsync();
        }

        Console.WriteLine("✓ Database schema initialized");
        Console.WriteLine("  └─ 9 tables created: tenants, categories, instructors, students, courses, lessons, enrollments, reviews, payments\n");
    }

    static async Task RunUdemyCloneDemoAsync(IEntityManager entityManager, TenantManager tenantManager)
    {
        await Demo1_CreateTenantsAsync(tenantManager);
        await Demo2_CreateCategoriesAndInstructorsAsync(entityManager, tenantManager);
        await Demo3_CreateCoursesAndLessonsAsync(entityManager, tenantManager);
        await Demo4_StudentEnrollmentsAsync(entityManager, tenantManager);
        await Demo5_ReviewsAndRatingsAsync(entityManager, tenantManager);
        await Demo6_ReportingAndAnalyticsAsync(entityManager, tenantManager);
    }

    static async Task Demo1_CreateTenantsAsync(TenantManager tenantManager)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Demo 1: Multi-Tenant Setup");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        var tenants = new[]
        {
            ("techcorp-edu", "TechCorp Education", "Pro"),
            ("startup-academy", "Startup Academy", "Enterprise"),
            ("freelance-hub", "Freelance Hub", "Free")
        };

        foreach (var (id, name, tier) in tenants)
        {
            await tenantManager.CreateTenantAsync(id, name);
            Console.WriteLine($"✓ Created tenant: {name} ({tier} tier)");
        }

        Console.WriteLine($"\n[Completed] {tenants.Length} tenants created with isolated data!\n");
    }

    static async Task Demo2_CreateCategoriesAndInstructorsAsync(IEntityManager entityManager, TenantManager tenantManager)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Demo 2: Categories & Instructors");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        await tenantManager.SetCurrentTenantAsync("techcorp-edu");

        // Create categories
        var webDevCategory = new Category
        {
            TenantId = "techcorp-edu",
            Name = "Web Development",
            Slug = "web-development",
            Description = "Learn modern web development",
            Icon = "code",
            IsActive = true
        };
        await entityManager.PersistAsync(webDevCategory);
        Console.WriteLine($"✓ Created category: {webDevCategory.Name}");

        var dataScienceCategory = new Category
        {
            TenantId = "techcorp-edu",
            Name = "Data Science",
            Slug = "data-science",
            Description = "Master data science and ML",
            Icon = "chart",
            IsActive = true
        };
        await entityManager.PersistAsync(dataScienceCategory);
        Console.WriteLine($"✓ Created category: {dataScienceCategory.Name}");

        // Create instructors
        var instructor1 = new Instructor
        {
            TenantId = "techcorp-edu",
            Email = "john.doe@techcorp.com",
            Name = "John Doe",
            Title = "Senior Software Engineer",
            Bio = "10+ years of web development experience",
            IsVerified = true
        };
        await entityManager.PersistAsync(instructor1);
        Console.WriteLine($"✓ Created instructor: {instructor1.Name}");

        var instructor2 = new Instructor
        {
            TenantId = "techcorp-edu",
            Email = "jane.smith@techcorp.com",
            Name = "Jane Smith",
            Title = "Data Scientist",
            Bio = "PhD in Machine Learning, 8 years industry experience",
            IsVerified = true
        };
        await entityManager.PersistAsync(instructor2);
        Console.WriteLine($"✓ Created instructor: {instructor2.Name}\n");
    }

    static async Task Demo3_CreateCoursesAndLessonsAsync(IEntityManager entityManager, TenantManager tenantManager)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Demo 3: Creating Courses & Lessons");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        await tenantManager.SetCurrentTenantAsync("techcorp-edu");

        // Get instructor
        var instructors = await entityManager.CreateQuery<Instructor>(
            "SELECT i FROM Instructor i WHERE i.Email = :email")
            .SetParameter("email", "john.doe@techcorp.com")
            .GetResultListAsync();
        var instructor = instructors.First();

        // Get category
        var categories = await entityManager.CreateQuery<Category>(
            "SELECT c FROM Category c WHERE c.Slug = :slug")
            .SetParameter("slug", "web-development")
            .GetResultListAsync();
        var category = categories.First();

        // Create course
        var course = new Course
        {
            TenantId = "techcorp-edu",
            InstructorId = instructor.Id,
            CategoryId = category.Id,
            Title = "Complete React & Next.js Masterclass 2025",
            Slug = "react-nextjs-masterclass-2025",
            ShortDescription = "Learn React 18 and Next.js 14 from scratch",
            Description = "Comprehensive course covering React, Next.js, TypeScript, and modern web development best practices.",
            Price = 89.99m,
            DiscountPrice = 49.99m,
            Level = "Intermediate",
            Language = "English",
            Status = "Published",
            IsFeatured = true,
            PublishedAt = DateTime.UtcNow
        };
        await entityManager.PersistAsync(course);
        Console.WriteLine($"✓ Created course: {course.Title}");
        Console.WriteLine($"  └─ Price: ${course.Price} (Discount: ${course.DiscountPrice})");

        // Create lessons
        var lessons = new[]
        {
            new Lesson { TenantId = "techcorp-edu", CourseId = course.Id, Title = "Introduction to React", DurationMinutes = 15, OrderIndex = 1, IsFreePreview = true, ContentType = "Video" },
            new Lesson { TenantId = "techcorp-edu", CourseId = course.Id, Title = "React Hooks Deep Dive", DurationMinutes = 45, OrderIndex = 2, ContentType = "Video" },
            new Lesson { TenantId = "techcorp-edu", CourseId = course.Id, Title = "Next.js App Router", DurationMinutes = 60, OrderIndex = 3, ContentType = "Video" },
            new Lesson { TenantId = "techcorp-edu", CourseId = course.Id, Title = "Server Components", DurationMinutes = 40, OrderIndex = 4, ContentType = "Video" },
            new Lesson { TenantId = "techcorp-edu", CourseId = course.Id, Title = "Final Project", DurationMinutes = 90, OrderIndex = 5, ContentType = "Project" }
        };

        foreach (var lesson in lessons)
        {
            await entityManager.PersistAsync(lesson);
        }

        // Update course stats
        course.TotalLessons = lessons.Length;
        course.DurationHours = lessons.Sum(l => l.DurationMinutes) / 60.0m;
        await entityManager.MergeAsync(course);

        Console.WriteLine($"  └─ Created {lessons.Length} lessons ({course.DurationHours:F1} hours total)\n");
    }

    static async Task Demo4_StudentEnrollmentsAsync(IEntityManager entityManager, TenantManager tenantManager)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Demo 4: Student Enrollments & Payments");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        await tenantManager.SetCurrentTenantAsync("techcorp-edu");

        // Create students
        var students = new[]
        {
            new Student { TenantId = "techcorp-edu", Email = "alice@example.com", Name = "Alice Johnson" },
            new Student { TenantId = "techcorp-edu", Email = "bob@example.com", Name = "Bob Williams" },
            new Student { TenantId = "techcorp-edu", Email = "carol@example.com", Name = "Carol Davis" }
        };

        foreach (var student in students)
        {
            await entityManager.PersistAsync(student);
            Console.WriteLine($"✓ Created student: {student.Name}");
        }

        // Get course
        var courses = await entityManager.CreateQuery<Course>(
            "SELECT c FROM Course c WHERE c.Slug = :slug")
            .SetParameter("slug", "react-nextjs-masterclass-2025")
            .GetResultListAsync();
        var course = courses.First();

        // Enroll students with payments
        foreach (var student in students)
        {
            // Create payment
            var payment = new Payment
            {
                TenantId = "techcorp-edu",
                StudentId = student.Id,
                CourseId = course.Id,
                Amount = course.DiscountPrice ?? course.Price,
                Currency = "USD",
                PaymentMethod = "CreditCard",
                TransactionId = $"TXN-{Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper()}",
                Status = "Completed",
                CompletedAt = DateTime.UtcNow
            };
            await entityManager.PersistAsync(payment);

            // Create enrollment
            var enrollment = new Enrollment
            {
                TenantId = "techcorp-edu",
                StudentId = student.Id,
                CourseId = course.Id,
                EnrolledAt = DateTime.UtcNow,
                ProgressPercentage = Random.Shared.Next(0, 100)
            };
            await entityManager.PersistAsync(enrollment);

            // Update student stats
            student.EnrolledCoursesCount++;
            student.LastActiveAt = DateTime.UtcNow;
            await entityManager.MergeAsync(student);

            Console.WriteLine($"  └─ {student.Name} enrolled (Payment: ${payment.Amount})");
        }

        // Update course stats
        course.EnrolledStudentsCount = students.Length;
        await entityManager.MergeAsync(course);

        Console.WriteLine($"\n[Completed] {students.Length} students enrolled in the course!\n");
    }

    static async Task Demo5_ReviewsAndRatingsAsync(IEntityManager entityManager, TenantManager tenantManager)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Demo 5: Reviews & Ratings System");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        await tenantManager.SetCurrentTenantAsync("techcorp-edu");

        // Get course and students
        var courses = await entityManager.CreateQuery<Course>(
            "SELECT c FROM Course c WHERE c.Slug = :slug")
            .SetParameter("slug", "react-nextjs-masterclass-2025")
            .GetResultListAsync();
        var course = courses.First();

        var allStudents = await entityManager.CreateQuery<Student>("SELECT s FROM Student s").GetResultListAsync();
        var students = allStudents.ToList();

        // Create reviews
        var reviews = new[]
        {
            new Review { TenantId = "techcorp-edu", CourseId = course.Id, StudentId = students[0].Id, Rating = 5, Title = "Excellent course!", Comment = "Very comprehensive and well-structured. Highly recommended!", IsVerifiedPurchase = true },
            new Review { TenantId = "techcorp-edu", CourseId = course.Id, StudentId = students[1].Id, Rating = 4, Title = "Good content", Comment = "Great content but could use more real-world examples.", IsVerifiedPurchase = true },
            new Review { TenantId = "techcorp-edu", CourseId = course.Id, StudentId = students[2].Id, Rating = 5, Title = "Best React course", Comment = "This is the best React course I've taken. John explains everything clearly.", IsVerifiedPurchase = true, HelpfulCount = 15 }
        };

        foreach (var review in reviews)
        {
            await entityManager.PersistAsync(review);
            Console.WriteLine($"✓ {review.Rating}★ review by Student#{review.StudentId}: {review.Title}");
        }

        // Calculate and update average rating
        var averageRating = reviews.Average(r => r.Rating);
        course.AverageRating = (decimal)averageRating;
        course.TotalReviews = reviews.Length;
        await entityManager.MergeAsync(course);

        Console.WriteLine($"\n[Completed] Course rating: {course.AverageRating:F1}★ ({course.TotalReviews} reviews)\n");
    }

    static async Task Demo6_ReportingAndAnalyticsAsync(IEntityManager entityManager, TenantManager tenantManager)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Demo 6: Reporting & Analytics Dashboard");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        await tenantManager.SetCurrentTenantAsync("techcorp-edu");

        // Get platform stats
        var totalCourses = await entityManager.CreateQuery<Course>(
            "SELECT COUNT(c) FROM Course c").ExecuteScalarAsync();

        var publishedCourses = await entityManager.CreateQuery<Course>(
            "SELECT COUNT(c) FROM Course c WHERE c.Status = 'Published'").ExecuteScalarAsync();

        var totalStudents = await entityManager.CreateQuery<Student>(
            "SELECT COUNT(s) FROM Student s").ExecuteScalarAsync();

        var totalEnrollments = await entityManager.CreateQuery<Enrollment>(
            "SELECT COUNT(e) FROM Enrollment e").ExecuteScalarAsync();

        var totalRevenue = await entityManager.CreateQuery<Payment>(
            "SELECT SUM(p.Amount) FROM Payment p WHERE p.Status = 'Completed'").ExecuteScalarAsync();

        var averageRating = await entityManager.CreateQuery<Course>(
            "SELECT AVG(c.AverageRating) FROM Course c WHERE c.Status = 'Published'").ExecuteScalarAsync();

        Console.WriteLine("📊 Platform Analytics (TechCorp Education):");
        Console.WriteLine($"   ├─ Total Courses: {totalCourses}");
        Console.WriteLine($"   ├─ Published Courses: {publishedCourses}");
        Console.WriteLine($"   ├─ Total Students: {totalStudents}");
        Console.WriteLine($"   ├─ Total Enrollments: {totalEnrollments}");
        Console.WriteLine($"   ├─ Total Revenue: ${totalRevenue:F2}");
        Console.WriteLine($"   └─ Average Course Rating: {averageRating:F2}★");

        // Top courses
        Console.WriteLine($"\n📈 Top Performing Courses:");
        var topCourses = await entityManager.CreateQuery<Course>(
            "SELECT c FROM Course c WHERE c.Status = 'Published' ORDER BY c.EnrolledStudentsCount DESC LIMIT 5")
            .GetResultListAsync();

        foreach (var course in topCourses)
        {
            Console.WriteLine($"   • {course.Title}");
            Console.WriteLine($"     └─ {course.EnrolledStudentsCount} students, {course.AverageRating:F1}★, ${course.Price}");
        }

        Console.WriteLine($"\n[Completed] Multi-tenant SaaS platform is fully operational!\n");
    }
}
