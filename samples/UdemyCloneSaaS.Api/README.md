# Udemy Clone SaaS API

ASP.NET Core Web API demonstrating the NPA Framework with automatic repository registration.

## Features

- [Completed] **Auto-Generated Repositories** - All repository implementations generated at compile-time
- [Completed] **AddNPA() Extension** - Single method call to register all repositories  
- [Completed] **AddPostgreSqlProvider()** - Fluent provider registration with connection management
- [Completed] **Testcontainers** - Automatic PostgreSQL container for development
- [Completed] **RESTful API** - Full CRUD operations for Students, Courses, and Enrollments
- [Completed] **Swagger/OpenAPI** - Interactive API documentation
- [Completed] **Dependency Injection** - Automatic DI registration via source generator

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Docker (for Testcontainers)

### Configuration

The API uses **Testcontainers** to automatically start a PostgreSQL container. No manual database setup required!

If you prefer to use an existing PostgreSQL database, update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=udemy_clone;Username=postgres;Password=yourpassword"
  }
}
```

And modify `Program.cs` to use the connection string instead of Testcontainers.

### Build and Run

```bash
dotnet build
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:5001/swagger`

## API Endpoints

### Students

- `GET /api/students` - Get all students
- `GET /api/students/{id}` - Get student by ID
- `GET /api/students/by-email/{email}` - Get student by email
- `POST /api/students` - Create a new student
- `PUT /api/students/{id}` - Update a student
- `DELETE /api/students/{id}` - Delete a student

### Courses

- `GET /api/courses` - Get all courses
- `GET /api/courses/{id}` - Get course by ID
- `GET /api/courses/by-instructor/{instructorId}` - Get courses by instructor
- `POST /api/courses` - Create a new course
- `PUT /api/courses/{id}` - Update a course
- `DELETE /api/courses/{id}` - Delete a course

### Enrollments

- `GET /api/enrollments` - Get all enrollments
- `GET /api/enrollments/{id}` - Get enrollment by ID
- `GET /api/enrollments/by-student/{studentId}` - Get enrollments by student
- `GET /api/enrollments/by-course/{courseId}` - Get enrollments by course
- `POST /api/enrollments` - Create a new enrollment
- `PUT /api/enrollments/{id}` - Update an enrollment
- `DELETE /api/enrollments/{id}` - Delete an enrollment

## How It Works

### 1. Testcontainers Setup

The API automatically starts a PostgreSQL container on startup:

```csharp
var postgresContainer = new PostgreSqlBuilder()
    .WithImage("postgres:16-alpine")
    .WithDatabase("udemy_clone")
    .WithUsername("postgres")
    .WithPassword("postgres")
    .Build();

await postgresContainer.StartAsync();
var connectionString = postgresContainer.GetConnectionString();
```

### 2. Provider Registration

Use the `AddPostgreSqlProvider()` extension method:

```csharp
// Register PostgreSQL provider with connection string
builder.Services.AddPostgreSqlProvider(connectionString);
```

This single line:
- Registers `IDbConnection` with scoped lifetime
- Registers `IDatabaseProvider` → `PostgreSqlProvider`
- Configures PostgreSQL-specific SQL dialect and type converters

### 3. Repository Interfaces (UdemyCloneSaaS Project)

Define repository interfaces with the `[Repository]` attribute:

```csharp
[Repository]
public interface IStudentRepository : IRepository<Student, long>
{
    [Query("SELECT s FROM Student s WHERE s.Email = :email")]
    Task<Student?> FindByEmailAsync(string email);
}
```

### 4. Source Generator Creates Implementations

The NPA source generator automatically creates:
- `StudentRepositoryImplementation` class
- `NPAServiceCollectionExtensions` with `AddNPA()` method

### 5. Register with One Line

In `Program.cs`:

```csharp
// Automatically registers IEntityManager and all repositories
builder.Services.AddNPA();
```

This single line registers:
- `IEntityManager` 
- `IStudentRepository` → `StudentRepositoryImplementation`
- `ICourseRepository` → `CourseRepositoryImplementation`
- `IEnrollmentRepository` → `EnrollmentRepositoryImplementation`
- And all other repositories in the project

### 6. Use in Controllers

```csharp
public class StudentsController : ControllerBase
{
    private readonly IStudentRepository _studentRepository;

    public StudentsController(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetAll()
    {
        var students = await _studentRepository.GetAllAsync();
        return Ok(students);
    }
}
```

## Generated Code

The NPA source generator creates:

### NPAServiceCollectionExtensions.g.cs

```csharp
public static class NPAServiceCollectionExtensions
{
    public static IServiceCollection AddNPA(this IServiceCollection services)
    {
        services.AddScoped<IEntityManager, EntityManager>();
        
        services.AddScoped<IStudentRepository, StudentRepositoryImplementation>();
        services.AddScoped<ICourseRepository, CourseRepositoryImplementation>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepositoryImplementation>();
        services.AddScoped<ICategoryRepository, CategoryRepositoryImplementation>();
        services.AddScoped<IInstructorRepository, InstructorRepositoryImplementation>();
        services.AddScoped<ILessonRepository, LessonRepositoryImplementation>();
        services.AddScoped<IPaymentRepository, PaymentRepositoryImplementation>();
        services.AddScoped<IReviewRepository, ReviewRepositoryImplementation>();
        
        return services;
    }
}
```

## Architecture

```
┌─────────────────────┐
│  Controllers        │
│  - StudentsController
│  - CoursesController
│  - EnrollmentsController
└──────────┬──────────┘
           │ DI injects
           ▼
┌─────────────────────┐
│  Repositories       │  (Generated)
│  - StudentRepositoryImplementation
│  - CourseRepositoryImplementation  
│  - EnrollmentRepositoryImplementation
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  Entity Manager     │
│  - NPA.Core
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  PostgreSQL         │
│  - Npgsql Provider
└─────────────────────┘
```

## Benefits

1. **Zero Boilerplate** - No manual repository implementations
2. **Type-Safe** - Compile-time code generation
3. **Easy Testing** - Standard dependency injection patterns
4. **Single Registration** - One `AddNPA()` call for everything
5. **Incremental** - Only regenerates when interfaces change

## Next Steps

- Add authentication and authorization
- Implement caching
- Add API versioning
- Configure CORS for frontend integration
- Add health checks
- Implement pagination for GET endpoints

## Related Projects

- [UdemyCloneSaaS](../UdemyCloneSaaS) - Console application with entities and repositories
- [NPA.Core](../../src/NPA.Core) - Core NPA framework
- [NPA.Generators](../../src/NPA.Generators) - Source generators
