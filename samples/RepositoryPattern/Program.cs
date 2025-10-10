using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NPA.Core.Annotations;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Core.Providers;
using NPA.Core.Repositories;
using NPA.Providers.PostgreSql;
using Npgsql;
using Testcontainers.PostgreSql;

namespace RepositoryPattern;

/// <summary>
/// Demonstrates the Repository Pattern implementation in NPA.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // Create PostgreSQL container
        var postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("npa_repo_demo")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithPortBinding(5432, true)
            .Build();
        
        var logger = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        }).CreateLogger<Program>();
        
        try
        {
            logger.LogInformation("Starting PostgreSQL container...");
            await postgresContainer.StartAsync();
            logger.LogInformation("PostgreSQL container started successfully.");
            
            var connectionString = postgresContainer.GetConnectionString();
            logger.LogInformation("Connection string: {ConnectionString}", connectionString);
            
            // Create database connection
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            
            // Create test table
            await CreateTestTable(connection, logger);
            
            // Build host with services
            var host = CreateHostBuilder(args, connectionString).Build();
            
        logger.LogInformation("Starting Repository Pattern Demo...");

            await RunRepositoryPatternDemo(host.Services, connection);
        
        logger.LogInformation("Repository Pattern Demo completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during Repository Pattern Demo");
        }
        finally
        {
            logger.LogInformation("Stopping PostgreSQL container...");
            await postgresContainer.StopAsync();
            await postgresContainer.DisposeAsync();
            logger.LogInformation("PostgreSQL container stopped.");
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args, string connectionString) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Configure logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
                
                // Configure NPA services
                services.AddSingleton<IMetadataProvider, MetadataProvider>();
                services.AddScoped<IDatabaseProvider, PostgreSqlProvider>();
                
                // Configure database connection - use singleton to ensure same connection
                services.AddSingleton<IDbConnection>(sp =>
                {
                    var conn = new NpgsqlConnection(connectionString);
                    conn.Open();
                    return conn;
                });
                
                // Configure EntityManager
                services.AddScoped<IEntityManager>(sp =>
                {
                    var connection = sp.GetRequiredService<IDbConnection>();
                    var metadata = sp.GetRequiredService<IMetadataProvider>();
                    var provider = sp.GetRequiredService<IDatabaseProvider>();
                    var logger = sp.GetRequiredService<ILogger<EntityManager>>();
                    return new EntityManager(connection, metadata, provider, logger);
                });
                
                // Register generic repository (fallback)
                services.AddScoped(typeof(IRepository<,>), typeof(BaseRepository<,>));
                
                // Register custom repositories
                services.AddScoped<IUserRepository, UserRepository>();
                
                // Register repository factory
                services.AddScoped<IRepositoryFactory, RepositoryFactory>();
            });
    
    static async Task CreateTestTable(NpgsqlConnection connection, ILogger logger)
    {
        logger.LogInformation("Creating test table...");
        
        const string createTableSql = @"
            CREATE TABLE IF NOT EXISTS users (
                id SERIAL PRIMARY KEY,
                username VARCHAR(100) NOT NULL UNIQUE,
                email VARCHAR(255) NOT NULL UNIQUE,
                created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                is_active BOOLEAN NOT NULL DEFAULT true
            );";
        
        await using var command = new NpgsqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
        
        logger.LogInformation("Test table created successfully.");
    }

    static async Task RunRepositoryPatternDemo(IServiceProvider services, NpgsqlConnection connection)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("=== NPA Repository Pattern Demo ===");
        logger.LogInformation("");
        
        // Demo 1: Basic Repository Operations
        await DemoBasicRepositoryOperations(services, connection, logger);
        
        // Demo 2: Custom Repository Methods
        await DemoCustomRepositoryMethods(services, logger);
        
        // Demo 3: LINQ Predicates
        await DemoLinqPredicates(services, logger);
        
        // Demo 4: Ordering and Paging
        await DemoOrderingAndPaging(services, logger);
        
        // Demo 5: Repository Factory
        await DemoRepositoryFactory(services, logger);
    }
    
    static async Task DemoBasicRepositoryOperations(IServiceProvider services, NpgsqlConnection connection, ILogger logger)
    {
        logger.LogInformation("--- Demo 1: Basic Repository Operations with Real Database ---");
        
        var userRepo = services.GetRequiredService<IUserRepository>();
        
        // Clear existing data
        await using (var cmd = new NpgsqlCommand("TRUNCATE TABLE users RESTART IDENTITY CASCADE", connection))
        {
            await cmd.ExecuteNonQueryAsync();
        }
        
        // Add a user
        var user = new User
        {
            Username = "john_doe",
            Email = "john@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        
        logger.LogInformation("Adding user: {Username}", user.Username);
        user = await userRepo.AddAsync(user);
        logger.LogInformation("User added with ID: {Id}", user.Id);
        
        // Get user by ID
        var foundUser = await userRepo.GetByIdAsync(user.Id);
        logger.LogInformation("Found user by ID {Id}: {Username}", user.Id, foundUser?.Username);
        
        // Check if exists
        var exists = await userRepo.ExistsAsync(user.Id);
        logger.LogInformation("User exists: {Exists}", exists);
        
        // Count users
        var count = await userRepo.CountAsync();
        logger.LogInformation("Total users: {Count}", count);
        
        // Update user
        user.Username = "john_doe_updated";
        await userRepo.UpdateAsync(user);
        logger.LogInformation("User updated");
        
        // Get all users
        var allUsers = await userRepo.GetAllAsync();
        logger.LogInformation("Total users retrieved: {Count}", allUsers.Count());
    }
    
    static async Task DemoCustomRepositoryMethods(IServiceProvider services, ILogger logger)
    {
        logger.LogInformation("");
        logger.LogInformation("--- Demo 2: Custom Repository Methods ---");
        
        var userRepo = services.GetRequiredService<IUserRepository>();
        
        logger.LogInformation("Custom repository methods available:");
        logger.LogInformation("  - FindByEmailDomainAsync(domain)");
        logger.LogInformation("  - FindRecentlyCreatedAsync(days)");
        logger.LogInformation("  - GetActiveUsersPagedAsync(page, pageSize)");
        
        await Task.CompletedTask;
    }
    
    static async Task DemoLinqPredicates(IServiceProvider services, ILogger logger)
    {
        logger.LogInformation("");
        logger.LogInformation("--- Demo 3: LINQ Predicates ---");
        
        var userRepo = services.GetRequiredService<IUserRepository>();
        
        logger.LogInformation("LINQ predicate examples:");
        logger.LogInformation("  - FindAsync(u => u.IsActive)");
        logger.LogInformation("  - FindAsync(u => u.Email.Contains(\"@example.com\"))");
        logger.LogInformation("  - FindAsync(u => u.CreatedAt > DateTime.Now.AddDays(-30))");
        
        await Task.CompletedTask;
    }
    
    static async Task DemoOrderingAndPaging(IServiceProvider services, ILogger logger)
    {
        logger.LogInformation("");
        logger.LogInformation("--- Demo 4: Ordering and Paging ---");
        
        var userRepo = services.GetRequiredService<IUserRepository>();
        
        logger.LogInformation("Ordering and paging examples:");
        logger.LogInformation("  - FindAsync(predicate, u => u.Username, descending: true)");
        logger.LogInformation("  - FindAsync(predicate, skip: 0, take: 10)");
        logger.LogInformation("  - FindAsync(predicate, orderBy, descending, skip, take)");
        
        await Task.CompletedTask;
    }
    
    static async Task DemoRepositoryFactory(IServiceProvider services, ILogger logger)
    {
        logger.LogInformation("");
        logger.LogInformation("--- Demo 5: Repository Factory ---");
        
        var factory = services.GetRequiredService<IRepositoryFactory>();
        
        logger.LogInformation("Creating repositories via factory:");
        logger.LogInformation("  - factory.CreateRepository<User, long>()");
        logger.LogInformation("  - factory.CreateRepository<User>()");
        
        // Create a generic repository
        var genericUserRepo = factory.CreateRepository<User, long>();
        logger.LogInformation($"Created repository type: {genericUserRepo.GetType().Name}");
        
        await Task.CompletedTask;
    }
}

/// <summary>
/// Custom user repository with domain-specific operations.
/// </summary>
public interface IUserRepository : IRepository<User, long>
{
    Task<IEnumerable<User>> FindByEmailDomainAsync(string domain);
    Task<IEnumerable<User>> FindRecentlyCreatedAsync(int days);
    Task<(IEnumerable<User> users, int totalCount)> GetActiveUsersPagedAsync(int page, int pageSize);
}

/// <summary>
/// Implementation of custom user repository using NPA.
/// </summary>
public class UserRepository : CustomRepositoryBase<User, long>, IUserRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class.
    /// </summary>
    public UserRepository(IDbConnection connection, IEntityManager entityManager, IMetadataProvider metadataProvider)
        : base(connection, entityManager, metadataProvider)
    {
    }
    
    /// <summary>
    /// Finds users by email domain asynchronously.
    /// </summary>
    public async Task<IEnumerable<User>> FindByEmailDomainAsync(string domain)
    {
        // Use LINQ predicate support from BaseRepository
        return await FindAsync(u => u.Email.Contains($"@{domain}"));
    }
    
    /// <summary>
    /// Finds recently created users asynchronously.
    /// </summary>
    public async Task<IEnumerable<User>> FindRecentlyCreatedAsync(int days)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        return await FindAsync(
            u => u.CreatedAt >= cutoffDate,
            u => u.CreatedAt,
            descending: true);
    }
    
    /// <summary>
    /// Gets active users with paging support.
    /// </summary>
    public async Task<(IEnumerable<User> users, int totalCount)> GetActiveUsersPagedAsync(int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        
        var users = await FindAsync(
            u => u.IsActive,
            u => u.Username,
            false,
            skip,
            pageSize);
        
        var totalCount = await CountAsync();
        
        return (users, totalCount);
    }
}

/// <summary>
/// User entity with NPA annotations.
/// </summary>
[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("username", IsNullable = false)]
    public string Username { get; set; } = string.Empty;
    
    [Column("email", IsNullable = false, IsUnique = true)]
    public string Email { get; set; } = string.Empty;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
