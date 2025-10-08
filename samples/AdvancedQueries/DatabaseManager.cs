using Npgsql;
using Testcontainers.PostgreSql;

namespace AdvancedQueries;

/// <summary>
/// Manages PostgreSQL database container lifecycle and schema initialization.
/// </summary>
public class DatabaseManager : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container;
    private NpgsqlConnection? _connection;

    public DatabaseManager()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("advancedqueries")
            .WithUsername("npa_user")
            .WithPassword("npa_password")
            .Build();
    }

    public async Task<NpgsqlConnection> StartAsync()
    {
        Console.WriteLine("Starting PostgreSQL container...");
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();
        _connection = new NpgsqlConnection(connectionString);
        await _connection.OpenAsync();

        await InitializeSchemaAsync();

        Console.WriteLine("Database ready!\n");
        return _connection;
    }

    private async Task InitializeSchemaAsync()
    {
        const string createTablesSql = @"
            -- Products table
            CREATE TABLE IF NOT EXISTS products (
                id BIGSERIAL PRIMARY KEY,
                name VARCHAR(200) NOT NULL,
                category VARCHAR(50) NOT NULL,
                price DECIMAL(18,2) NOT NULL,
                stock_quantity INTEGER NOT NULL,
                supplier_id BIGINT,
                is_active BOOLEAN NOT NULL DEFAULT true,
                created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            -- Orders table
            CREATE TABLE IF NOT EXISTS orders (
                id BIGSERIAL PRIMARY KEY,
                order_number VARCHAR(50) NOT NULL UNIQUE,
                customer_name VARCHAR(100) NOT NULL,
                order_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                total_amount DECIMAL(18,2) NOT NULL,
                status VARCHAR(20) NOT NULL DEFAULT 'Pending',
                shipped_date TIMESTAMP
            );

            -- Create indexes for common queries
            CREATE INDEX IF NOT EXISTS idx_products_category ON products(category);
            CREATE INDEX IF NOT EXISTS idx_products_price ON products(price);
            CREATE INDEX IF NOT EXISTS idx_products_supplier ON products(supplier_id);
            CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(status);
            CREATE INDEX IF NOT EXISTS idx_orders_date ON orders(order_date);
        ";

        await using var command = new NpgsqlCommand(createTablesSql, _connection);
        await command.ExecuteNonQueryAsync();
        
        Console.WriteLine("Database schema and indexes created successfully");
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        await _container.StopAsync();
        await _container.DisposeAsync();
    }
}
