using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.Annotations;
using NPA.Core.Core;
using NPA.Core.Extensions;
using NPA.Core.Repositories;
using NPA.Providers.PostgreSql.Extensions;
using NPA.Samples.Core;
using NPA.Samples.Entities;
using Npgsql;
using System.Data;
using Testcontainers.PostgreSql;

namespace NPA.Samples.Features;

public class RepositoryPatternSample : ISample
{
    public string Name => "Repository Pattern";
    public string Description => "Demonstrates base repositories, custom repositories, and LINQ-like queries.";

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

            await using var serviceProvider = services.BuildServiceProvider();

            await CreateDatabaseSchemaAsync(connectionString);

            var userRepo = serviceProvider.GetRequiredService<IUserRepository>();

            // 1. Add users
            Console.WriteLine("\n1. Adding new users via repository...");
            await userRepo.AddAsync(new User { Username = "jane.doe", Email = "jane.doe@example.com", IsActive = true, CreatedAt = DateTime.UtcNow });
            await userRepo.AddAsync(new User { Username = "john.smith", Email = "john.smith@email.com", IsActive = false, CreatedAt = DateTime.UtcNow.AddDays(-1) });
            Console.WriteLine("   > 2 users added.");

            // 2. Get a user by ID
            Console.WriteLine("\n2. Getting a user by ID...");
            var user = await userRepo.GetByIdAsync(1L);
            Console.WriteLine($"   > Found user: {user?.Username}");

            // 3. Use a custom repository method
            Console.WriteLine("\n3. Finding users with a custom repository method...");
            var emailDomain = "example.com";
            var users = await userRepo.FindByEmailDomainAsync(emailDomain);
            Console.WriteLine($"   > Found {users.Count()} user(s) with '@{emailDomain}' domain.");

            // 4. Use a LINQ-like FindAsync predicate
            Console.WriteLine("\n4. Finding inactive users with a LINQ-like predicate...");
            var activeUsers = await userRepo.FindAsync(u => !u.IsActive);
            Console.WriteLine($"   > Found {activeUsers.Count()} inactive user(s).");
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
    
    
    // --- Supporting classes for this sample ---

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

}
