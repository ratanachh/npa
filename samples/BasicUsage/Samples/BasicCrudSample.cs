using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.Core;
using NPA.Core.Extensions;
using NPA.Providers.PostgreSql.Extensions;
using NPA.Samples.Core;
using Npgsql;
using Testcontainers.PostgreSql;

namespace NPA.Samples.Features;

public class BasicCrudSample : ISample
{
    public string Name => "Basic CRUD Operations";
    public string Description => "Demonstrates basic entity mapping, EntityManager CRUD operations, and simple CPQL queries using the PostgreSQL provider.";

    public async Task RunAsync()
    {
        var postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("npa_basic_crud")
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

            await using var serviceProvider = services.BuildServiceProvider();

            await CreateDatabaseSchemaAsync(connectionString);

            var entityManager = serviceProvider.GetRequiredService<IEntityManager>();

            // 1. Persist a new entity
            Console.WriteLine("\n1. Persisting a new User...");
            var newUser = new User { Username = "john.doe", Email = "john.doe@example.com", IsActive = true, CreatedAt = DateTime.UtcNow };
            await entityManager.PersistAsync(newUser);
            Console.WriteLine($"   > User persisted with ID: {newUser.Id}");

            // 2. Find the entity
            Console.WriteLine("\n2. Finding the user by ID...");
            var foundUser = await entityManager.FindAsync<User>(newUser.Id);
            Console.WriteLine($"   > Found user: {foundUser?.Username}");

            // 3. Merge (update) the entity
            Console.WriteLine("\n3. Merging (updating) the user...");
            foundUser!.Email = "john.doe.updated@example.com";
            await entityManager.MergeAsync(foundUser);
            Console.WriteLine("   > User email updated.");

            // 4. Verify the update
            var updatedUser = await entityManager.FindAsync<User>(newUser.Id);
            Console.WriteLine($"   > Verified updated email: {updatedUser?.Email}");

            // 5. Query using CPQL
            Console.WriteLine("\n5. Querying for active users with CPQL...");
            var activeUsers = await entityManager.CreateQuery<User>("SELECT u FROM User u WHERE u.IsActive = :isActive")
                .SetParameter("isActive", true)
                .GetResultListAsync();
            Console.WriteLine($"   > Found {activeUsers.Count()} active user(s).");

            // 6. Remove the entity
            Console.WriteLine("\n6. Removing the user...");
            await entityManager.RemoveAsync(updatedUser!);
            var deletedUser = await entityManager.FindAsync<User>(newUser.Id);
            Console.WriteLine($"   > User after deletion: {(deletedUser == null ? "Not Found" : "Found")}");
        }
    }

    private async Task CreateDatabaseSchemaAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        const string createTableSql = @"
            CREATE TABLE IF NOT EXISTS users (
                id BIGSERIAL PRIMARY KEY,
                username VARCHAR(255) NOT NULL,
                email VARCHAR(255) NOT NULL,
                created_at TIMESTAMP NOT NULL,
                is_active BOOLEAN NOT NULL
            )";

        await using var command = new NpgsqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
        
        Console.WriteLine("Database schema created successfully");
    }
}
