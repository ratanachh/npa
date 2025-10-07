using DotNet.Testcontainers.Builders;
using Testcontainers.MsSql;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;

namespace NPA.Integration.Tests;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected MsSqlContainer? SqlServerContainer { get; private set; }
    protected MySqlContainer? MySqlContainer { get; private set; }
    protected PostgreSqlContainer? PostgreSqlContainer { get; private set; }

    public virtual async Task InitializeAsync()
    {
        // Initialize test containers if needed
        // This is a base class - specific tests can override to set up specific databases
        await Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        if (SqlServerContainer != null)
            await SqlServerContainer.DisposeAsync();
        
        if (MySqlContainer != null)
            await MySqlContainer.DisposeAsync();
        
        if (PostgreSqlContainer != null)
            await PostgreSqlContainer.DisposeAsync();
    }

    protected async Task<MsSqlContainer> CreateSqlServerContainerAsync()
    {
        SqlServerContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();

        await SqlServerContainer.StartAsync();
        return SqlServerContainer;
    }

    protected async Task<MySqlContainer> CreateMySqlContainerAsync()
    {
        MySqlContainer = new MySqlBuilder()
            .WithImage("mysql:8.0")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(3306))
            .Build();

        await MySqlContainer.StartAsync();
        return MySqlContainer;
    }

    protected async Task<PostgreSqlContainer> CreatePostgreSqlContainerAsync()
    {
        PostgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();

        await PostgreSqlContainer.StartAsync();
        return PostgreSqlContainer;
    }
}