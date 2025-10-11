# NPA Samples

This directory contains sample applications demonstrating various features of the NPA (JPA-like ORM for .NET) library.

## 📁 Sample Projects

### 1. **BasicUsage** ✅ Complete
Demonstrates basic entity mapping, CRUD operations, and queries with all three database providers.

**Features:**
- Entity mapping with attributes
- CRUD operations (Create, Read, Update, Delete)
- CPQL queries (NPA's query language)
- Multiple database providers (SQL Server, MySQL, PostgreSQL)
- Relationship mapping examples
- **NEW:** Synchronous vs Asynchronous methods comparison

**Run it:**
```bash
cd BasicUsage

# Default (SQL Server)
dotnet run

# With specific provider
dotnet run sqlserver
dotnet run mysql
dotnet run postgresql

# Show sync vs async comparison
dotnet run --sync-async
```

### 2. **ConsoleAppSync** ✅ New (with Testcontainers)
Console application demonstrating **synchronous methods** for CLI tools, scripts, and batch processing.

**Features:**
- Synchronous CRUD operations
- Synchronous query execution
- Batch operations
- **Testcontainers integration** (SQL Server in Docker)
- No external database required!
- Follows same pattern as SqlServerProviderRunner

**Run it:**
```bash
# Requires Docker Desktop running
cd ConsoleAppSync
dotnet run
```

**What it does:**
1. 🐳 Starts SQL Server container automatically
2. 📊 Runs sync method demos
3. 🧹 Cleans up container on exit

**When to use:**
- Console applications
- CLI tools and scripts
- Batch processing jobs
- Desktop applications (WPF, WinForms)
- Database utilities

**Technologies:**
- Testcontainers.MsSql 3.6.0
- Dependency Injection
- SQL Server in Docker

### 3. **AdvancedQueries** ✅ Updated (Phase 2.3)
Demonstrates **advanced CPQL features** including JOINs, GROUP BY, HAVING, and complex expressions.

**Features:**
- ✅ JOIN operations (INNER, LEFT, RIGHT, FULL)
- ✅ GROUP BY and HAVING clauses
- ✅ Aggregate functions (COUNT, SUM, AVG, MIN, MAX) with DISTINCT
- ✅ String functions (UPPER, LOWER, LENGTH, SUBSTRING, TRIM, CONCAT)
- ✅ Date functions (YEAR, MONTH, DAY, HOUR, MINUTE, SECOND, NOW)
- ✅ Complex expressions with operator precedence
- ✅ DISTINCT and multiple ORDER BY
- ✅ Parameterized queries with named parameters
- 17 comprehensive query examples

**Run it:**
```bash
cd AdvancedQueries
dotnet run
```

### 4. **SourceGeneratorDemo** ✅ Complete (Phase 1.6 & 2.6)
Demonstrates **both** NPA source generators for automatic code generation at compile time.

**Features:**
- **Repository Generator (Phase 1.6):**
  - Automatic repository implementation from interfaces
  - Convention-based method generation
  - Type-safe CRUD operations
  - Zero runtime overhead
  
- **Metadata Generator (Phase 2.6):** ⭐ NEW
  - Compile-time entity metadata generation
  - Zero reflection at runtime (10-100x faster)
  - Automatic entity discovery
  - Pre-computed property information
  - Generated `GeneratedMetadataProvider` class

**Run it:**
```bash
cd SourceGeneratorDemo
dotnet run
```

**Generated Files:**
```
obj/Debug/net8.0/generated/
├── NPA.Generators.RepositoryGenerator/
│   └── UserRepositoryImplementation.g.cs
└── NPA.Generators.EntityMetadataGenerator/
    └── GeneratedMetadataProvider.g.cs
```

### 5. **RepositoryPattern** 🚧 Partial
Demonstrates manual repository pattern implementation.

**Status:** Basic structure, needs completion in Phase 2.4

### 6. **WebApplication** 🚧 Partial
ASP.NET Core web application demonstrating **asynchronous methods** for web APIs.

**Features:**
- RESTful API with NPA
- Asynchronous operations
- Dependency injection
- Controller-based architecture

**Status:** Basic structure, needs completion in Phase 2.4

**When to use:**
- ASP.NET Core web applications
- Web APIs and microservices
- High-concurrency scenarios
- Real-time applications

## 📚 Documentation

Each sample includes its own README.md with:
- Purpose and goals
- Features demonstrated
- Setup instructions
- Code examples
- Related concepts

## ⚖️ Sync vs Async - Quick Guide

### Use **Asynchronous Methods** (await/Task) ✅
```csharp
var user = await entityManager.FindAsync<User>(id);
await entityManager.PersistAsync(user);
```

**Best for:**
- ASP.NET Core web applications
- Web APIs and microservices
- High-concurrency services
- Azure Functions
- Any I/O-bound operations in web context

**Why:** Frees up threads during database I/O, allowing better scalability

### Use **Synchronous Methods** (blocking) ✅
```csharp
var user = entityManager.Find<User>(id);
entityManager.Persist(user);
```

**Best for:**
- Console applications
- CLI tools and scripts
- Batch processing jobs
- Desktop applications (WPF, WinForms)
- Simple CRUD utilities
- Database migration scripts

**Why:** Simpler code flow, no async complexity needed

## 🚀 Getting Started

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-org/npa.git
   cd npa/samples
   ```

2. **Build all samples**
   ```bash
   dotnet build
   ```

3. **Run a specific sample**
   ```bash
   cd BasicUsage
   dotnet run
   ```

## 🎯 Learning Path

**Recommended order:**

1. **BasicUsage** - Start here to learn the basics
2. **ConsoleAppSync** - Learn synchronous methods
3. **AdvancedQueries** - Deep dive into queries
4. **SourceGeneratorDemo** - Explore code generation
5. **WebApplication** - Build web APIs with NPA

## 📖 Additional Resources

- [Main README](../README.md) - Project overview
- [Getting Started Guide](../docs/GettingStarted.md) - Detailed setup
- [API Documentation](../README.md#-api-reference) - Complete API reference

## 💡 Tips

- All samples support multiple database providers
- Check connection strings in Program.cs files
- Each sample is self-contained and can run independently
- Use `--help` flag on samples that support it
- Check the console output for detailed explanations

## 🤝 Contributing

Found a bug or want to improve a sample? Contributions are welcome!

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## 📄 License

MIT License - see [LICENSE](../LICENSE) for details
