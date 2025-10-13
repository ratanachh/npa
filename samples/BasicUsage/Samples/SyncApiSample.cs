using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPA.Core.Core;
using NPA.Core.Extensions;
using NPA.Providers.SqlServer.Extensions;
using NPA.Samples.Core;
using NPA.Samples.Entities;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace NPA.Samples.Features;

public class SyncApiSample : ISample
{
    public string Name => "Synchronous API Usage";
    public string Description => "Demonstrates the use of synchronous (blocking) API methods, ideal for console applications.";

    public async Task RunAsync()
    {
        var sqlServerContainer = new MsSqlBuilder()
            .WithPassword("YourStrong@Passw0rd")
            .WithCleanUp(true)
            .Build();

        await using (sqlServerContainer)
        {
            Console.WriteLine("Starting SQL Server container...");
            await sqlServerContainer.StartAsync();
            Console.WriteLine("SQL Server container started.");

            var connectionString = sqlServerContainer.GetConnectionString();

            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            services.AddSqlServerProvider(connectionString);

            await using var serviceProvider = services.BuildServiceProvider();

            await CreateDatabaseSchemaAsync(connectionString);

            var entityManager = serviceProvider.GetRequiredService<IEntityManager>();

            RunCrudOperations(entityManager);
            RunQueryOperations(entityManager);
        }
    }

    private void RunCrudOperations(IEntityManager entityManager)
    {
        Console.WriteLine("\n--- CRUD Operations (Synchronous) ---");

        // CREATE
        Console.WriteLine("1. Creating new customer...");
        var customer = new Customer { Name = "Jane Doe", Email = "jane.doe@example.com", Phone = "555-1234", CreatedAt = DateTime.UtcNow, IsActive = true };
        entityManager.Persist(customer);
        Console.WriteLine($"   > Created customer ID: {customer.Id}");

        // READ
        Console.WriteLine("\n2. Finding customer...");
        var foundCustomer = entityManager.Find<Customer>(customer.Id);
        Console.WriteLine($"   > Found: {foundCustomer?.Name}");

        // UPDATE
        Console.WriteLine("\n3. Updating customer...");
        foundCustomer!.Email = "jane.doe.updated@example.com";
        entityManager.Merge(foundCustomer);
        Console.WriteLine("   > Updated email.");

        // DELETE
        Console.WriteLine("\n4. Deleting customer...");
        entityManager.Remove(foundCustomer);
        var deletedCustomer = entityManager.Find<Customer>(customer.Id);
        Console.WriteLine($"   > Customer after deletion: {(deletedCustomer == null ? "Not Found" : "Found")}");
    }

    private void RunQueryOperations(IEntityManager entityManager)
    {
        Console.WriteLine("\n--- Query Operations (Synchronous) ---");

        // CREATE
        entityManager.Persist(new Customer { Name = "Alice", Email = "alice@example.com", CreatedAt = DateTime.UtcNow, IsActive = true });
        entityManager.Persist(new Customer { Name = "Bob", Email = "bob@example.com", CreatedAt = DateTime.UtcNow, IsActive = false });

        // QUERY
        Console.WriteLine("1. Finding all active customers...");
        var activeCustomers = entityManager.CreateQuery<Customer>("SELECT c FROM Customer c WHERE c.IsActive = :isActive")
            .SetParameter("isActive", true)
            .GetResultList();
        Console.WriteLine($"   > Found {activeCustomers.Count()} active customer(s).");
    }

    private async Task CreateDatabaseSchemaAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        const string createTableSql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'customers')
            BEGIN
                CREATE TABLE customers (
                    id BIGINT PRIMARY KEY IDENTITY(1,1),
                    name NVARCHAR(100) NOT NULL,
                    email NVARCHAR(255) NOT NULL,
                    phone NVARCHAR(20) NULL,
                    created_at DATETIME2 NOT NULL,
                    is_active BIT NOT NULL DEFAULT 1
                );
            END";

        await using var command = new SqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
        Console.WriteLine("Database schema created successfully");
    }
}
