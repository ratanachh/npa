using Microsoft.Extensions.DependencyInjection;
using NPA.Core.Core;
using NPA.Providers.PostgreSql.Extensions;
using NPA.Samples.Core;
using Npgsql;
using Testcontainers.PostgreSql;

namespace NPA.Samples.Samples;

/// <summary>
/// Runner for cascade operations sample.
/// Sets up PostgreSQL database with Testcontainers and runs all cascade demos.
/// </summary>
public class CascadeSampleRunner : ISample
{
    public string Name => "Cascade Operations (Phase 3.2)";
    public string Description => "Demonstrates cascade persist, merge, remove, and orphan removal";

    public async Task RunAsync()
    {
        // Start PostgreSQL container
        var postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("npa_cascade_demo")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .WithCleanUp(true)
            .Build();

        await using (postgres)
        {
            Console.WriteLine("Starting PostgreSQL container...");
            await postgres.StartAsync();
            Console.WriteLine("PostgreSQL container started.");

            var connectionString = postgres.GetConnectionString();

            // Setup dependency injection
            var services = new ServiceCollection();
            services.AddPostgreSqlProvider(connectionString);

            var serviceProvider = services.BuildServiceProvider();
            var entityManager = serviceProvider.GetRequiredService<IEntityManager>();

            // Initialize database schema
            await InitializeDatabaseAsync(connectionString);

            // Run cascade demos
            var sample = new CascadeSample(entityManager);

            await sample.Demo1_CascadePersist();
            await sample.Demo2_CascadeMerge();
            await sample.Demo3_CascadeRemove();
            await sample.Demo4_OrphanRemoval();
            await sample.Demo5_CascadeAll();
            await sample.Demo6_NoCascade();

            Console.WriteLine("\n✓ All cascade operation demos completed successfully!");

            // Wait for user input before returning to menu
            Console.WriteLine("\nPress any key to return to the menu...");
            Console.ReadKey();
        }
    }

    private async Task InitializeDatabaseAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            -- Drop existing tables
            DROP TABLE IF EXISTS cascade_team_members CASCADE;
            DROP TABLE IF EXISTS cascade_teams CASCADE;
            DROP TABLE IF EXISTS cascade_tasks CASCADE;
            DROP TABLE IF EXISTS cascade_projects CASCADE;
            DROP TABLE IF EXISTS cascade_employees CASCADE;
            DROP TABLE IF EXISTS cascade_companies CASCADE;
            DROP TABLE IF EXISTS cascade_departments CASCADE;

            -- Create departments table
            CREATE TABLE cascade_departments (
                id BIGSERIAL PRIMARY KEY,
                name VARCHAR(255) NOT NULL
            );

            -- Create companies table
            CREATE TABLE cascade_companies (
                id BIGSERIAL PRIMARY KEY,
                name VARCHAR(255) NOT NULL
            );

            -- Create employees table (used by both departments and companies)
            CREATE TABLE cascade_employees (
                id BIGSERIAL PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                position VARCHAR(255) NOT NULL,
                salary DECIMAL(18,2) NOT NULL,
                department_id BIGINT REFERENCES cascade_departments(id) ON DELETE CASCADE,
                company_id BIGINT REFERENCES cascade_companies(id) ON DELETE CASCADE
            );

            -- Create projects table
            CREATE TABLE cascade_projects (
                id BIGSERIAL PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                description TEXT
            );

            -- Create tasks table
            CREATE TABLE cascade_tasks (
                id BIGSERIAL PRIMARY KEY,
                title VARCHAR(255) NOT NULL,
                status VARCHAR(50) NOT NULL,
                project_id BIGINT REFERENCES cascade_projects(id) ON DELETE CASCADE
            );

            -- Create teams table (no cascade in FK)
            CREATE TABLE cascade_teams (
                id BIGSERIAL PRIMARY KEY,
                name VARCHAR(255) NOT NULL
            );

            -- Create team members table (no cascade in FK)
            CREATE TABLE cascade_team_members (
                id BIGSERIAL PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                role VARCHAR(100) NOT NULL,
                team_id BIGINT REFERENCES cascade_teams(id)
            );
        ";

        await command.ExecuteNonQueryAsync();
        Console.WriteLine("Database schema initialized");
    }
}
