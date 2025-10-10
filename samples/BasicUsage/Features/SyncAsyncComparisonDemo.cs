using System.Data;
using Microsoft.Data.SqlClient;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Providers.SqlServer;
using Testcontainers.MsSql;

namespace BasicUsage.Features;

/// <summary>
/// Demonstrates both synchronous and asynchronous methods in NPA.
/// Shows when to use each approach and compares the patterns side-by-side.
/// Uses Testcontainers for fully isolated demo - no external database required!
/// </summary>
public static class SyncAsyncComparisonDemo
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n=== Sync vs Async Comparison Demo ===");
        Console.WriteLine("Using Testcontainers (SQL Server in Docker)");
        Console.WriteLine("This demo shows the difference between synchronous and asynchronous methods.\n");

        MsSqlContainer? container = null;
        SqlConnection? connection = null;

        try
        {
            // Start SQL Server container
            Console.WriteLine("🐳 Starting SQL Server container...");
            container = new MsSqlBuilder()
                .WithPassword("YourStrong@Passw0rd")
                .WithCleanUp(true)
                .Build();

            await container.StartAsync();
            Console.WriteLine("✓ SQL Server container started\n");

            // Connect to container
            var connectionString = container.GetConnectionString();
            connection = new SqlConnection(connectionString);
            connection.Open();
            Console.WriteLine("✓ Connected to containerized database\n");

            var metadataProvider = new MetadataProvider();
            var databaseProvider = new SqlServerProvider();
            var entityManager = new EntityManager(connection, metadataProvider, databaseProvider);

            // Create table
            await EnsureTableExistsAsync(connection);

            Console.WriteLine("--- 1. ASYNCHRONOUS METHODS (Recommended for Web Apps) ---\n");
            await DemonstrateAsyncMethodsAsync(entityManager);

            Console.WriteLine("\n--- 2. SYNCHRONOUS METHODS (For Console Apps, Scripts) ---\n");
            DemonstrateSyncMethods(entityManager);

            Console.WriteLine("\n--- 3. QUERY METHODS COMPARISON ---\n");
            await CompareQueryMethodsAsync(entityManager);

            Console.WriteLine("\n=== Demo Complete ===\n");
        }
        finally
        {
            // Cleanup
            if (connection != null)
            {
                connection.Close();
                connection.Dispose();
                Console.WriteLine("✓ Database connection closed");
            }

            if (container != null)
            {
                await container.StopAsync();
                await container.DisposeAsync();
                Console.WriteLine("✓ SQL Server container stopped and removed\n");
            }
        }
    }

    private static async Task DemonstrateAsyncMethodsAsync(IEntityManager entityManager)
    {
        Console.WriteLine("Using ASYNC methods with await/Task:\n");

        // CREATE - Async
        Console.WriteLine("1. Creating user (async)...");
        var asyncUser = new User
        {
            Username = "async_user",
            Email = "async@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await entityManager.PersistAsync(asyncUser);
        await entityManager.FlushAsync();
        Console.WriteLine($"   ✓ Created user with ID: {asyncUser.Id}");

        // READ - Async
        Console.WriteLine("\n2. Finding user (async)...");
        var foundUser = await entityManager.FindAsync<User>(asyncUser.Id);
        Console.WriteLine($"   ✓ Found: {foundUser?.Username} - {foundUser?.Email}");

        // UPDATE - Async
        Console.WriteLine("\n3. Updating user (async)...");
        if (foundUser != null)
        {
            foundUser.Email = "updated_async@example.com";
            await entityManager.MergeAsync(foundUser);
            await entityManager.FlushAsync();
            Console.WriteLine($"   ✓ Updated email to: {foundUser.Email}");
        }

        // QUERY - Async
        Console.WriteLine("\n4. Querying users (async)...");
        var activeUsers = await entityManager
            .CreateQuery<User>("SELECT u FROM User u WHERE u.IsActive = :active")
            .SetParameter("active", true)
            .GetResultListAsync();
        Console.WriteLine($"   ✓ Found {activeUsers.Count()} active users");

        // DELETE - Async
        Console.WriteLine("\n5. Deleting user (async)...");
        if (foundUser != null)
        {
            await entityManager.RemoveAsync(foundUser);
            await entityManager.FlushAsync();
            Console.WriteLine($"   ✓ Deleted user ID: {foundUser.Id}");
        }
    }

    private static void DemonstrateSyncMethods(IEntityManager entityManager)
    {
        Console.WriteLine("Using SYNC methods (blocking, no await):\n");

        // CREATE - Sync
        Console.WriteLine("1. Creating user (sync)...");
        var syncUser = new User
        {
            Username = "sync_user",
            Email = "sync@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        entityManager.Persist(syncUser);
        entityManager.Flush();
        Console.WriteLine($"   ✓ Created user with ID: {syncUser.Id}");

        // READ - Sync
        Console.WriteLine("\n2. Finding user (sync)...");
        var foundUser = entityManager.Find<User>(syncUser.Id);
        Console.WriteLine($"   ✓ Found: {foundUser?.Username} - {foundUser?.Email}");

        // UPDATE - Sync
        Console.WriteLine("\n3. Updating user (sync)...");
        if (foundUser != null)
        {
            foundUser.Email = "updated_sync@example.com";
            entityManager.Merge(foundUser);
            entityManager.Flush();
            Console.WriteLine($"   ✓ Updated email to: {foundUser.Email}");
        }

        // QUERY - Sync
        Console.WriteLine("\n4. Querying users (sync)...");
        var activeUsers = entityManager
            .CreateQuery<User>("SELECT u FROM User u WHERE u.IsActive = :active")
            .SetParameter("active", true)
            .GetResultList();
        Console.WriteLine($"   ✓ Found {activeUsers.Count()} active users");

        // DELETE - Sync
        Console.WriteLine("\n5. Deleting user (sync)...");
        if (foundUser != null)
        {
            entityManager.Remove(foundUser);
            entityManager.Flush();
            Console.WriteLine($"   ✓ Deleted user ID: {foundUser.Id}");
        }
    }

    private static async Task CompareQueryMethodsAsync(IEntityManager entityManager)
    {
        // Create test data
        Console.WriteLine("Creating test data...");
        for (int i = 1; i <= 5; i++)
        {
            var user = new User
            {
                Username = $"test_user_{i}",
                Email = $"test{i}@example.com",
                CreatedAt = DateTime.UtcNow,
                IsActive = i % 2 == 0
            };
            entityManager.Persist(user);
        }
        entityManager.Flush();

        // ASYNC Query Methods
        Console.WriteLine("\nASYNC Query Methods:");
        
        var asyncList = await entityManager
            .CreateQuery<User>("SELECT u FROM User u WHERE u.Username LIKE :pattern")
            .SetParameter("pattern", "test_user_%")
            .GetResultListAsync();
        Console.WriteLine($"  GetResultListAsync(): {asyncList.Count()} users");

        var asyncSingle = await entityManager
            .CreateQuery<User>("SELECT u FROM User u WHERE u.Username = :username")
            .SetParameter("username", "test_user_1")
            .GetSingleResultAsync();
        Console.WriteLine($"  GetSingleResultAsync(): {asyncSingle?.Username ?? "null"}");

        var asyncCount = await entityManager
            .CreateQuery<User>("SELECT COUNT(u) FROM User u WHERE u.IsActive = :active")
            .SetParameter("active", true)
            .ExecuteScalarAsync();
        Console.WriteLine($"  ExecuteScalarAsync(): {asyncCount} active users");

        // SYNC Query Methods
        Console.WriteLine("\nSYNC Query Methods:");
        
        var syncList = entityManager
            .CreateQuery<User>("SELECT u FROM User u WHERE u.Username LIKE :pattern")
            .SetParameter("pattern", "test_user_%")
            .GetResultList();
        Console.WriteLine($"  GetResultList(): {syncList.Count()} users");

        var syncSingle = entityManager
            .CreateQuery<User>("SELECT u FROM User u WHERE u.Username = :username")
            .SetParameter("username", "test_user_1")
            .GetSingleResult();
        Console.WriteLine($"  GetSingleResult(): {syncSingle?.Username ?? "null"}");

        var syncCount = entityManager
            .CreateQuery<User>("SELECT COUNT(u) FROM User u WHERE u.IsActive = :active")
            .SetParameter("active", true)
            .ExecuteScalar();
        Console.WriteLine($"  ExecuteScalar(): {syncCount} active users");

        // Cleanup
        Console.WriteLine("\nCleaning up test data...");
        entityManager
            .CreateQuery<User>("DELETE User u WHERE u.Username LIKE :pattern")
            .SetParameter("pattern", "test_user_%")
            .ExecuteUpdate();
    }

    private static async Task EnsureTableExistsAsync(IDbConnection connection)
    {
        var createTableSql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'users')
            BEGIN
                CREATE TABLE users (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    username NVARCHAR(50) NOT NULL,
                    email NVARCHAR(255) NOT NULL,
                    created_at DATETIME2 NOT NULL,
                    is_active BIT NOT NULL DEFAULT 1
                );
            END";

        if (connection is SqlConnection sqlConnection)
        {
            using var command = new SqlCommand(createTableSql, sqlConnection);
            await command.ExecuteNonQueryAsync();
        }
        else
        {
            using var command = connection.CreateCommand();
            command.CommandText = createTableSql;
            command.ExecuteNonQuery();
        }
    }
}

