using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.Annotations;
using NPA.Core.Core;
using NPA.Core.Extensions;
using NPA.Core.Repositories;
using NPA.Providers.PostgreSql.Extensions;
using NPA.Samples.Core;
using Npgsql;
using System.Data;
using Testcontainers.PostgreSql;

namespace NPA.Samples.Features;

public class RepositoryPatternSample : ISample
{
    public string Name => "Repository Pattern";
    public string Description => "Demonstrates base repositories, custom repositories, and the repository factory.";

    public async Task RunAsync()
    {
        var postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("npa_repo_demo")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithCleanUp(true)
            .Build();

        await using (postgresContainer)
        {
            Console.WriteLine("Starting PostgreSQL container...");
            await postgresContainer.StartAsync();
            Console.WriteLine("PostgreSQL container started.");

            var connectionString = postgresContainer.GetConnectionString();

            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            services.AddPostgreSqlProvider(connectionString);
            services.AddScoped(typeof(IRepository<,>), typeof(BaseRepository<,>));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRepositoryFactory, RepositoryFactory>();

            await using var serviceProvider = services.BuildServiceProvider();

            await CreateDatabaseSchemaAsync(connectionString);

            var userRepo = serviceProvider.GetRequiredService<IUserRepository>();

            // 1. Add a user
            Console.WriteLine("\n1. Adding a new user via repository...");
            var newUser = new User { Username = "jane.doe", Email = "jane.doe@example.com", IsActive = true, CreatedAt = DateTime.UtcNow };
            await userRepo.AddAsync(newUser);
            Console.WriteLine($"   > User added with ID: {newUser.Id}");

            // 2. Find user by custom method
            Console.WriteLine("\n2. Finding user by email domain...");
            var users = await userRepo.FindByEmailDomainAsync("example.com");
            Console.WriteLine($"   > Found {users.Count()} user(s) with '@example.com' domain.");

            // 3. Use LINQ-like FindAsync
            Console.WriteLine("\n3. Finding active users with LINQ-like predicate...");
            var activeUsers = await userRepo.FindAsync(u => u.IsActive);
            Console.WriteLine($"   > Found {activeUsers.Count()} active user(s).");
        }
    }

    private async Task CreateDatabaseSchemaAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

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
        Console.WriteLine("Database schema created successfully");
    }
}

// --- Supporting classes for this sample ---

[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("username")]
    public string Username { get; set; } = string.Empty;
    
    [Column("email")]
    public string Email { get; set; } = string.Empty;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}

public interface IUserRepository : IRepository<User, long>
{
    Task<IEnumerable<User>> FindByEmailDomainAsync(string domain);
}

public class UserRepository : BaseRepository<User, long>, IUserRepository
{
    public UserRepository(IDbConnection connection, IEntityManager entityManager, NPA.Core.Metadata.IMetadataProvider metadataProvider)
        : base(connection, entityManager, metadataProvider)
    {
    }

    public async Task<IEnumerable<User>> FindByEmailDomainAsync(string domain)
    {
        return await FindAsync(u => u.Email.Contains($"@{domain}"));
    }
}
