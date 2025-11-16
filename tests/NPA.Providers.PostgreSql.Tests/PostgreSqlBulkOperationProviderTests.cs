using FluentAssertions;
using Npgsql;
using NPA.Core.Annotations;
using NPA.Core.Metadata;
using NPA.Providers.PostgreSql;
using Testcontainers.PostgreSql;
using Xunit;

namespace NPA.Providers.PostgreSql.Tests;

/// <summary>
/// Collection definition to prevent parallel test execution for bulk operation tests.
/// </summary>
[CollectionDefinition("PostgreSQL Bulk Operations Tests", DisableParallelization = true)]
public class PostgreSqlBulkOperationTestsCollection
{
}

/// <summary>
/// Tests for PostgreSQL bulk operations provider.
/// These tests verify the temp table approach and connection handling using Testcontainers.
/// </summary>
[Collection("PostgreSQL Bulk Operations Tests")]
[Trait("Category", "Integration")]
public class PostgreSqlBulkOperationProviderTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly PostgreSqlBulkOperationProvider _provider;
    private readonly EntityMetadata _productMetadata;
    private NpgsqlConnection _connection = null!;

    public PostgreSqlBulkOperationProviderTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("npa_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithPortBinding(5432, true)
            .Build();
        
        var dialect = new PostgreSqlDialect();
        var typeConverter = new PostgreSqlTypeConverter();
        _provider = new PostgreSqlBulkOperationProvider(dialect, typeConverter);
        _productMetadata = CreateProductMetadata();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        
        var connectionString = _postgresContainer.GetConnectionString();
        _connection = new NpgsqlConnection(connectionString);
        await _connection.OpenAsync();
    }

    public async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task BulkUpdateAsync_WithValidData_ShouldUpdateRecordsSuccessfully()
    {
        // Arrange
        await CreateProductTableAsync(_connection);

        // Insert initial test data
        var initialProducts = new[]
        {
            new Product { Id = 1, Name = "Product 1", Price = 10.00m, Stock = 100 },
            new Product { Id = 2, Name = "Product 2", Price = 20.00m, Stock = 200 },
            new Product { Id = 3, Name = "Product 3", Price = 30.00m, Stock = 300 }
        };

        await InsertProductsAsync(_connection, initialProducts);

        // Prepare updated data
        var updatedProducts = new[]
        {
            new Product { Id = 1, Name = "Updated Product 1", Price = 15.00m, Stock = 150 },
            new Product { Id = 2, Name = "Updated Product 2", Price = 25.00m, Stock = 250 },
            new Product { Id = 3, Name = "Updated Product 3", Price = 35.00m, Stock = 350 }
        };

        // Act
        var result = await _provider.BulkUpdateAsync(
            _connection,
            updatedProducts,
            _productMetadata
        );

        // Assert
        result.Should().Be(3);

        // Verify the updates
        var verifiedProducts = await GetProductsAsync(_connection);
        verifiedProducts.Should().HaveCount(3);
        verifiedProducts[0].Name.Should().Be("Updated Product 1");
        verifiedProducts[0].Price.Should().Be(15.00m);
        verifiedProducts[0].Stock.Should().Be(150);
        verifiedProducts[1].Name.Should().Be("Updated Product 2");
        verifiedProducts[2].Name.Should().Be("Updated Product 3");

        // Cleanup
        await DropProductTableAsync(_connection);
    }

    [Fact]
    public async Task BulkUpdateAsync_WithLargeDataset_ShouldHandleTempTableCorrectly()
    {
        // Arrange
        await CreateProductTableAsync(_connection);

        // Insert 1000 products
        var products = Enumerable.Range(1, 1000)
            .Select(i => new Product
            {
                Id = i,
                Name = $"Product {i}",
                Price = i * 10.00m,
                Stock = i * 100
            })
            .ToArray();

        await InsertProductsAsync(_connection, products);

        // Prepare bulk update - update all prices to double
        var updatedProducts = products.Select(p => new Product
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price * 2,
            Stock = p.Stock
        }).ToArray();

        // Act
        var result = await _provider.BulkUpdateAsync(
            _connection,
            updatedProducts,
            _productMetadata
        );

        // Assert
        result.Should().Be(1000);

        // Verify random samples
        var verifiedProducts = await GetProductsAsync(_connection);
        verifiedProducts.Should().HaveCount(1000);
        verifiedProducts[0].Price.Should().Be(20.00m); // Original was 10, doubled to 20
        verifiedProducts[499].Price.Should().Be(10000.00m); // Original was 5000, doubled to 10000

        // Cleanup
        await DropProductTableAsync(_connection);
    }

    [Fact]
    public async Task BulkUpdateAsync_WithInvalidConnection_ShouldThrowException()
    {
        // Arrange
        await using var invalidConnection = new NpgsqlConnection("Host=invalid;Database=invalid");
        var products = new[] { new Product { Id = 1, Name = "Test", Price = 10, Stock = 100 } };

        // Act & Assert
        // The provider wraps exceptions in InvalidOperationException
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _provider.BulkUpdateAsync(invalidConnection, products, _productMetadata)
        );
    }

    [Fact]
    public async Task BulkUpdateAsync_ShouldUseSameTempTableSession()
    {
        // This test verifies the fix for "relation temp_bulk_products_xxx does not exist"
        // It ensures temp table and COPY command use the same connection instance

        // Arrange
        await CreateProductTableAsync(_connection);

        var products = new[]
        {
            new Product { Id = 1, Name = "Test Product", Price = 99.99m, Stock = 50 }
        };

        await InsertProductsAsync(_connection, products);

        var updatedProducts = new[]
        {
            new Product { Id = 1, Name = "Updated Test", Price = 199.99m, Stock = 100 }
        };

        // Act - This should not throw "relation does not exist"
        var result = await _provider.BulkUpdateAsync(
            _connection,
            updatedProducts,
            _productMetadata
        );

        // Assert
        result.Should().Be(1);

        // Verify update succeeded
        var verified = await GetProductsAsync(_connection);
        verified[0].Price.Should().Be(199.99m);

        // Cleanup
        await DropProductTableAsync(_connection);
    }

    [Fact]
    public async Task BulkInsertAsync_WithValidData_ShouldInsertRecordsSuccessfully()
    {
        // Arrange
        await CreateProductTableAsync(_connection);

        var products = new[]
        {
            new Product { Id = 1, Name = "New Product 1", Price = 10.00m, Stock = 100 },
            new Product { Id = 2, Name = "New Product 2", Price = 20.00m, Stock = 200 },
            new Product { Id = 3, Name = "New Product 3", Price = 30.00m, Stock = 300 }
        };

        // Act
        var result = await _provider.BulkInsertAsync(
            _connection,
            products,
            _productMetadata
        );

        // Assert
        result.Should().Be(3);

        // Verify inserts
        var verifiedProducts = await GetProductsAsync(_connection);
        verifiedProducts.Should().HaveCount(3);
        verifiedProducts[0].Name.Should().Be("New Product 1");
        verifiedProducts[1].Name.Should().Be("New Product 2");
        verifiedProducts[2].Name.Should().Be("New Product 3");

        // Cleanup
        await DropProductTableAsync(_connection);
    }

    [Fact]
    public async Task BulkInsertAsync_WithLargeDataset_ShouldUseCopyCommand()
    {
        // Arrange
        await CreateProductTableAsync(_connection);

        // Create 5000 products
        var products = Enumerable.Range(1, 5000)
            .Select(i => new Product
            {
                Id = i,
                Name = $"Bulk Product {i}",
                Price = i * 1.5m,
                Stock = i * 10
            })
            .ToArray();

        // Act
        var result = await _provider.BulkInsertAsync(
            _connection,
            products,
            _productMetadata
        );

        // Assert
        result.Should().Be(5000);

        // Verify count
        var count = await GetProductCountAsync(_connection);
        count.Should().Be(5000);

        // Cleanup
        await DropProductTableAsync(_connection);
    }

    [Fact]
    public async Task BulkDeleteAsync_WithValidIds_ShouldDeleteRecordsSuccessfully()
    {
        // Arrange
        await CreateProductTableAsync(_connection);

        var products = new[]
        {
            new Product { Id = 1, Name = "Product 1", Price = 10.00m, Stock = 100 },
            new Product { Id = 2, Name = "Product 2", Price = 20.00m, Stock = 200 },
            new Product { Id = 3, Name = "Product 3", Price = 30.00m, Stock = 300 },
            new Product { Id = 4, Name = "Product 4", Price = 40.00m, Stock = 400 },
            new Product { Id = 5, Name = "Product 5", Price = 50.00m, Stock = 500 }
        };

        await InsertProductsAsync(_connection, products);

        // Act - Delete products with IDs 2, 3, 4
        var idsToDelete = new object[] { 2, 3, 4 };
        var result = await _provider.BulkDeleteAsync(
            _connection,
            idsToDelete,
            _productMetadata
        );

        // Assert
        result.Should().Be(3);

        // Verify only products 1 and 5 remain
        var remainingProducts = await GetProductsAsync(_connection);
        remainingProducts.Should().HaveCount(2);
        remainingProducts[0].Id.Should().Be(1);
        remainingProducts[1].Id.Should().Be(5);

        // Cleanup
        await DropProductTableAsync(_connection);
    }

    [Fact]
    public async Task BulkDeleteAsync_WithLargeDataset_ShouldDeleteManyRecords()
    {
        // Arrange
        await CreateProductTableAsync(_connection);

        // Insert 10,000 products
        var products = Enumerable.Range(1, 10000)
            .Select(i => new Product
            {
                Id = i,
                Name = $"Product {i}",
                Price = i * 1.5m,
                Stock = i * 10
            })
            .ToArray();

        await InsertProductsAsync(_connection, products);

        // Act - Delete first 5,000 products
        var idsToDelete = Enumerable.Range(1, 5000).Cast<object>().ToArray();
        var result = await _provider.BulkDeleteAsync(
            _connection,
            idsToDelete,
            _productMetadata
        );

        // Assert
        result.Should().Be(5000);

        // Verify 5,000 products remain
        var count = await GetProductCountAsync(_connection);
        count.Should().Be(5000);

        // Cleanup
        await DropProductTableAsync(_connection);
    }

    private EntityMetadata CreateProductMetadata()
    {
        var metadata = new EntityMetadata
        {
            EntityType = typeof(Product),
            TableName = "products",
            SchemaName = "public"
        };

        metadata.Properties.Add("Id", new PropertyMetadata
        {
            PropertyName = "Id",
            ColumnName = "Id",
            PropertyType = typeof(int),
            IsPrimaryKey = true,
            GenerationType = GenerationType.None,
            IsNullable = false
        });

        metadata.Properties.Add("Name", new PropertyMetadata
        {
            PropertyName = "Name",
            ColumnName = "Name",
            PropertyType = typeof(string),
            IsPrimaryKey = false,
            IsNullable = false,
            Length = 255
        });

        metadata.Properties.Add("Price", new PropertyMetadata
        {
            PropertyName = "Price",
            ColumnName = "Price",
            PropertyType = typeof(decimal),
            IsPrimaryKey = false,
            IsNullable = false
        });

        metadata.Properties.Add("Stock", new PropertyMetadata
        {
            PropertyName = "Stock",
            ColumnName = "Stock",
            PropertyType = typeof(int),
            IsPrimaryKey = false,
            IsNullable = false
        });

        return metadata;
    }

    private async Task CreateProductTableAsync(NpgsqlConnection connection)
    {
        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS public.products (
                ""Id"" INTEGER PRIMARY KEY,
                ""Name"" VARCHAR(255) NOT NULL,
                ""Price"" DECIMAL(18,2) NOT NULL,
                ""Stock"" INTEGER NOT NULL
            );";

        await using var command = new NpgsqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task DropProductTableAsync(NpgsqlConnection connection)
    {
        var dropTableSql = "DROP TABLE IF EXISTS public.products;";
        await using var command = new NpgsqlCommand(dropTableSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task InsertProductsAsync(NpgsqlConnection connection, Product[] products)
    {
        foreach (var product in products)
        {
            var insertSql = @"
                INSERT INTO public.products (""Id"", ""Name"", ""Price"", ""Stock"")
                VALUES (@Id, @Name, @Price, @Stock);";

            await using var command = new NpgsqlCommand(insertSql, connection);
            command.Parameters.AddWithValue("Id", product.Id);
            command.Parameters.AddWithValue("Name", product.Name);
            command.Parameters.AddWithValue("Price", product.Price);
            command.Parameters.AddWithValue("Stock", product.Stock);
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task<List<Product>> GetProductsAsync(NpgsqlConnection connection)
    {
        var products = new List<Product>();
        var selectSql = @"SELECT ""Id"", ""Name"", ""Price"", ""Stock"" FROM public.products ORDER BY ""Id"";";

        await using var command = new NpgsqlCommand(selectSql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(new Product
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Price = reader.GetDecimal(2),
                Stock = reader.GetInt32(3)
            });
        }

        return products;
    }

    private async Task<int> GetProductCountAsync(NpgsqlConnection connection)
    {
        var countSql = "SELECT COUNT(*) FROM public.products;";
        await using var command = new NpgsqlCommand(countSql, connection);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    [Entity]
    [Table("products", Schema = "public")]
    private class Product
    {
        [Id]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        [Column("Price")]
        public decimal Price { get; set; }

        [Column("Stock")]
        public int Stock { get; set; }
    }
}
