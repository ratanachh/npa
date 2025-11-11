# Udemy Clone SaaS - Real-World Multi-Tenant Learning Platform

A comprehensive, production-ready sample application demonstrating how to build a **multi-tenant Software-as-a-Service (SaaS)** learning platform similar to Udemy using the **NPA Framework**.

## 🎯 Overview

This sample showcases a complete e-learning platform with:

- **Multi-Tenancy**: Each organization (tenant) has completely isolated data
- **Complex Domain Model**: 9 entities with relationships
- **Payment Processing**: Simulated payment system for course purchases
- **Review & Rating System**: Student feedback and course ratings
- **Progress Tracking**: Monitor student learning progress
- **Repository Pattern**: Auto-generated repositories using NPA source generators
- **Business Logic Services**: Clean separation of concerns

## 🏗️ Architecture

### Domain Model

```
┌─────────────┐
│   Tenant    │ (Organization using the platform)
└─────────────┘
       │
       ├─────► Categories (Web Dev, Data Science, etc.)
       ├─────► Instructors (Course creators)
       ├─────► Students (Learners)
       └─────► Courses
                 │
                 ├─────► Lessons (Videos, Articles, Quizzes)
                 ├─────► Enrollments (Student progress)
                 ├─────► Reviews (Ratings & feedback)
                 └─────► Payments (Transactions)
```

### Entities

1. **Tenant** - SaaS organization (TechCorp Education, Startup Academy)
2. **Category** - Course categories (Web Development, Data Science)
3. **Instructor** - Course creators with profiles and stats
4. **Student** - Learners with enrollment and progress tracking
5. **Course** - Educational content with pricing and metadata
6. **Lesson** - Individual course content (videos, articles, quizzes)
7. **Enrollment** - Student-course relationship with progress
8. **Review** - Student ratings and feedback (1-5 stars)
9. **Payment** - Transaction records for course purchases

### Key Features Demonstrated

#### 1. Multi-Tenancy with Data Isolation
```csharp
[Entity]
[Table("courses")]
[MultiTenant]  // Automatic tenant filtering
public class Course
{
    [Column("tenant_id")]
    public string TenantId { get; set; }
    // All queries automatically filtered by tenant!
}
```

#### 2. Source-Generated Repositories
```csharp
[Repository]
public interface ICourseRepository : IRepository<Course, long>
{
    // Auto-implemented CRUD methods from IRepository<Course, long>
    
    // Custom queries auto-generated
    [Query("SELECT c FROM Course c WHERE c.Status = 'Published' ORDER BY c.EnrolledStudentsCount DESC LIMIT :limit")]
    Task<IEnumerable<Course>> GetMostPopularCoursesAsync(int limit);
}
```

#### 3. Business Logic Services
```csharp
public class EnrollmentService
{
    public async Task<Enrollment> EnrollStudentAsync(long studentId, long courseId, Payment? payment = null)
    {
        // Validate, create enrollment, process payment
        // Update stats, issue certificate, etc.
    }
}
```

#### 4. Complex Queries and Aggregations
```csharp
// Get total revenue
var totalRevenue = await entityManager
    .CreateQuery<Payment>("SELECT SUM(p.Amount) FROM Payment p WHERE p.Status = 'Completed'")
    .ExecuteScalarAsync();

// Get average course rating
var avgRating = await entityManager
    .CreateQuery<Course>("SELECT AVG(c.AverageRating) FROM Course c WHERE c.Status = 'Published'")
    .ExecuteScalarAsync();
```

## 🚀 Running the Sample

### Prerequisites

- .NET 8.0 SDK
- Docker (for PostgreSQL testcontainer)

### Run

```bash
cd samples/UdemyCloneSaaS
dotnet run
```

### What It Does

The sample automatically:

1. **Starts PostgreSQL** in a testcontainer
2. **Initializes database schema** (9 tables with indexes)
3. **Creates 3 tenants** (TechCorp Education, Startup Academy, Freelance Hub)
4. **Demonstrates 6 scenarios**:
   - Multi-tenant setup
   - Categories & instructors creation
   - Courses & lessons authoring
   - Student enrollments & payments
   - Reviews & rating system
   - Analytics dashboard

### Expected Output

```
╔════════════════════════════════════════════════════════════════╗
║          Udemy Clone SaaS - Multi-Tenant Learning Platform    ║
║                  Built with NPA Framework                      ║
╚════════════════════════════════════════════════════════════════╝

Starting PostgreSQL container...
✓ PostgreSQL container started

Initializing database schema...
✓ Database schema initialized
  └─ 9 tables created: tenants, categories, instructors, students, courses...

═══════════════════════════════════════════════════════════════
Demo 1: Multi-Tenant Setup
═══════════════════════════════════════════════════════════════

✓ Created tenant: TechCorp Education (Pro tier)
✓ Created tenant: Startup Academy (Enterprise tier)
✓ Created tenant: Freelance Hub (Free tier)

[Completed] 3 tenants created with isolated data!

═══════════════════════════════════════════════════════════════
Demo 2: Categories & Instructors
═══════════════════════════════════════════════════════════════

✓ Created category: Web Development
✓ Created category: Data Science
✓ Created instructor: John Doe
✓ Created instructor: Jane Smith

═══════════════════════════════════════════════════════════════
Demo 3: Creating Courses & Lessons
═══════════════════════════════════════════════════════════════

✓ Created course: Complete React & Next.js Masterclass 2025
  └─ Price: $89.99 (Discount: $49.99)
  └─ Created 5 lessons (4.2 hours total)

═══════════════════════════════════════════════════════════════
Demo 4: Student Enrollments & Payments
═══════════════════════════════════════════════════════════════

✓ Created student: Alice Johnson
✓ Created student: Bob Williams
✓ Created student: Carol Davis
  └─ Alice Johnson enrolled (Payment: $49.99)
  └─ Bob Williams enrolled (Payment: $49.99)
  └─ Carol Davis enrolled (Payment: $49.99)

[Completed] 3 students enrolled in the course!

═══════════════════════════════════════════════════════════════
Demo 5: Reviews & Ratings System
═══════════════════════════════════════════════════════════════

✓ 5★ review by Student#1: Excellent course!
✓ 4★ review by Student#2: Good content
✓ 5★ review by Student#3: Best React course

[Completed] Course rating: 4.7★ (3 reviews)

═══════════════════════════════════════════════════════════════
Demo 6: Reporting & Analytics Dashboard
═══════════════════════════════════════════════════════════════

📊 Platform Analytics (TechCorp Education):
   ├─ Total Courses: 1
   ├─ Published Courses: 1
   ├─ Total Students: 3
   ├─ Total Enrollments: 3
   ├─ Total Revenue: $149.97
   └─ Average Course Rating: 4.67★

📈 Top Performing Courses:
   • Complete React & Next.js Masterclass 2025
     └─ 3 students, 4.7★, $89.99

[Completed] Multi-tenant SaaS platform is fully operational!
```

## 📁 Project Structure

```
UdemyCloneSaaS/
├── Entities/              # Domain entities
│   ├── Tenant.cs
│   ├── Category.cs
│   ├── Instructor.cs
│   ├── Student.cs
│   ├── Course.cs
│   ├── Lesson.cs
│   ├── Enrollment.cs
│   ├── Review.cs
│   └── Payment.cs
│
├── Repositories/          # Repository interfaces (auto-implemented)
│   ├── ICourseRepository.cs
│   ├── IEnrollmentRepository.cs
│   ├── IInstructorRepository.cs
│   ├── IStudentRepository.cs
│   ├── IReviewRepository.cs
│   ├── ILessonRepository.cs
│   ├── IPaymentRepository.cs
│   └── ICategoryRepository.cs
│
├── Services/              # Business logic services
│   ├── CourseService.cs
│   ├── EnrollmentService.cs
│   ├── ReviewService.cs
│   └── PaymentService.cs
│
├── Program.cs             # Main demo runner
├── UdemyCloneSaaS.csproj
└── README.md
```

## 🎓 Learning Outcomes

By studying this sample, you'll learn:

### 1. Multi-Tenancy Patterns
- Automatic tenant isolation with `[MultiTenant]` attribute
- Tenant context management
- Data isolation and security

### 2. Domain-Driven Design
- Rich domain model with business rules
- Entity relationships and navigation
- Aggregate roots and value objects

### 3. Repository Pattern
- Interface-based repositories
- Source generator magic
- Custom query methods with `[Query]` attribute

### 4. Service Layer
- Business logic separation
- Transaction management
- Cross-entity coordination

### 5. Real-World Scenarios
- Payment processing workflows
- Review and rating systems
- Progress tracking
- Analytics and reporting

## 🔧 Extending the Sample

### Add New Features

1. **Course Bundles**: Group courses into packages
2. **Coupons & Discounts**: Promotional code system
3. **Live Sessions**: Real-time video teaching
4. **Certificates**: PDF generation for completions
5. **Quizzes**: Assessment system within lessons
6. **Discussion Forums**: Student-instructor communication
7. **Wishlists**: Students save courses for later
8. **Recommendations**: AI-powered course suggestions

### Scale to Production

1. **Add Authentication**: OAuth2/OIDC integration
2. **Add Authorization**: Role-based access control (RBAC)
3. **Add Caching**: Redis for performance
4. **Add File Storage**: S3/Azure Blob for videos
5. **Add Email**: SendGrid for notifications
6. **Add Search**: Elasticsearch for course discovery
7. **Add Monitoring**: Application Insights/Prometheus
8. **Add CDN**: CloudFlare for video delivery

## 💡 Best Practices Demonstrated

[Completed] **Multi-tenant isolation** - Automatic data segregation  
[Completed] **Source generators** - Type-safe, zero-boilerplate repositories  
[Completed] **Clean architecture** - Entities → Repositories → Services  
[Completed] **Rich domain model** - Business logic in entities  
[Completed] **Query optimization** - Indexes on foreign keys and filters  
[Completed] **ACID transactions** - EntityManager handles consistency  
[Completed] **Testable code** - Dependency injection throughout  

## 📊 Database Schema Highlights

- **9 tables** with proper relationships
- **Indexes** on tenant_id, foreign keys, and common queries
- **Check constraints** for data validation (rating 1-5)
- **Cascade deletes** for lessons when course deleted
- **Unique constraints** for email per tenant

## 🎯 Use Cases Covered

1. **Organization Onboarding** - Create tenant, set limits
2. **Course Authoring** - Instructor creates course with lessons
3. **Student Registration** - Sign up, browse catalog
4. **Course Purchase** - Payment processing and enrollment
5. **Learning** - Track progress, complete lessons
6. **Feedback** - Write reviews and rate courses
7. **Analytics** - Revenue reports, popularity metrics

## 🚀 Production Readiness

This sample demonstrates patterns suitable for production:

- [Completed] Multi-tenant data isolation
- [Completed] Payment transaction handling
- [Completed] Progress tracking
- [Completed] Review moderation ready
- [Completed] Scalable repository pattern
- [Completed] Service layer for business logic
- [Completed] Analytics and reporting queries

## 📝 License

This sample is part of the NPA Framework samples collection and follows the same license as the main project.

---

**Built with ❤️ using NPA Framework**
