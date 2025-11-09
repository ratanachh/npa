using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NPA.Core.Annotations;
using NPA.Monitoring.Audit;
using Xunit;

namespace NPA.Monitoring.Tests;

public class AuditLoggingTests
{
    [Fact]
    public async Task WriteAsync_ShouldAddAuditEntry()
    {
        // Arrange
        var store = new InMemoryAuditStore(NullLogger<InMemoryAuditStore>.Instance);
        var entry = new AuditEntry
        {
            Action = "Create",
            EntityType = "User",
            EntityId = "123",
            User = "admin@test.com"
        };

        // Act
        await store.WriteAsync(entry);

        // Assert
        store.Count.Should().Be(1);
        var entries = await store.GetByEntityAsync("User", "123");
        entries.Should().HaveCount(1);
        entries.First().Action.Should().Be("Create");
    }

    [Fact]
    public async Task QueryAsync_WithNoFilter_ShouldReturnAll()
    {
        // Arrange
        var store = new InMemoryAuditStore(NullLogger<InMemoryAuditStore>.Instance);
        await store.WriteAsync(new AuditEntry { Action = "Create", EntityType = "User" });
        await store.WriteAsync(new AuditEntry { Action = "Update", EntityType = "Product" });

        // Act
        var results = await store.QueryAsync(new AuditFilter());

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task QueryAsync_WithUserFilter_ShouldFilterByUser()
    {
        // Arrange
        var store = new InMemoryAuditStore(NullLogger<InMemoryAuditStore>.Instance);
        await store.WriteAsync(new AuditEntry { Action = "Create", User = "user1@test.com" });
        await store.WriteAsync(new AuditEntry { Action = "Update", User = "user2@test.com" });
        await store.WriteAsync(new AuditEntry { Action = "Delete", User = "user1@test.com" });

        // Act
        var results = await store.QueryAsync(new AuditFilter { User = "user1@test.com" });

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(e => e.User.Should().Be("user1@test.com"));
    }

    [Fact]
    public async Task QueryAsync_WithDateRange_ShouldFilterByDate()
    {
        // Arrange
        var store = new InMemoryAuditStore(NullLogger<InMemoryAuditStore>.Instance);
        var yesterday = DateTime.UtcNow.AddDays(-1);
        var tomorrow = DateTime.UtcNow.AddDays(1);

        await store.WriteAsync(new AuditEntry { Action = "Create", Timestamp = yesterday });
        await store.WriteAsync(new AuditEntry { Action = "Update", Timestamp = DateTime.UtcNow });
        await store.WriteAsync(new AuditEntry { Action = "Delete", Timestamp = tomorrow });

        // Act
        var results = await store.QueryAsync(new AuditFilter 
        { 
            StartDate = DateTime.UtcNow.AddHours(-1),
            EndDate = DateTime.UtcNow.AddHours(1)
        });

        // Assert
        results.Should().HaveCount(1);
        results.First().Action.Should().Be("Update");
    }

    [Fact]
    public async Task QueryAsync_WithEntityType_ShouldFilterByEntityType()
    {
        // Arrange
        var store = new InMemoryAuditStore(NullLogger<InMemoryAuditStore>.Instance);
        await store.WriteAsync(new AuditEntry { EntityType = "User", Action = "Create" });
        await store.WriteAsync(new AuditEntry { EntityType = "Product", Action = "Create" });
        await store.WriteAsync(new AuditEntry { EntityType = "User", Action = "Update" });

        // Act
        var results = await store.QueryAsync(new AuditFilter { EntityType = "User" });

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(e => e.EntityType.Should().Be("User"));
    }

    [Fact]
    public async Task QueryAsync_WithAction_ShouldFilterByAction()
    {
        // Arrange
        var store = new InMemoryAuditStore(NullLogger<InMemoryAuditStore>.Instance);
        await store.WriteAsync(new AuditEntry { Action = "Create", EntityType = "User" });
        await store.WriteAsync(new AuditEntry { Action = "Update", EntityType = "User" });
        await store.WriteAsync(new AuditEntry { Action = "Delete", EntityType = "User" });

        // Act
        var results = await store.QueryAsync(new AuditFilter { Action = "Update" });

        // Assert
        results.Should().HaveCount(1);
        results.First().Action.Should().Be("Update");
    }

    [Fact]
    public async Task QueryAsync_WithCategory_ShouldFilterByCategory()
    {
        // Arrange
        var store = new InMemoryAuditStore(NullLogger<InMemoryAuditStore>.Instance);
        await store.WriteAsync(new AuditEntry { Category = "Security", Action = "Login" });
        await store.WriteAsync(new AuditEntry { Category = "Data", Action = "Create" });
        await store.WriteAsync(new AuditEntry { Category = "Security", Action = "Logout" });

        // Act
        var results = await store.QueryAsync(new AuditFilter { Category = "Security" });

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(e => e.Category.Should().Be("Security"));
    }

    [Fact]
    public async Task QueryAsync_WithSeverity_ShouldFilterBySeverity()
    {
        // Arrange
        var store = new InMemoryAuditStore(NullLogger<InMemoryAuditStore>.Instance);
        await store.WriteAsync(new AuditEntry { Severity = AuditSeverity.Low });
        await store.WriteAsync(new AuditEntry { Severity = AuditSeverity.High });
        await store.WriteAsync(new AuditEntry { Severity = AuditSeverity.Critical });

        // Act
        var results = await store.QueryAsync(new AuditFilter { Severity = AuditSeverity.High });

        // Assert
        results.Should().HaveCount(1);
        results.First().Severity.Should().Be(AuditSeverity.High);
    }

    [Fact]
    public async Task QueryAsync_WithMaxResults_ShouldLimitResults()
    {
        // Arrange
        var store = new InMemoryAuditStore(NullLogger<InMemoryAuditStore>.Instance);
        for (int i = 0; i < 10; i++)
        {
            await store.WriteAsync(new AuditEntry { Action = $"Action{i}" });
        }

        // Act
        var results = await store.QueryAsync(new AuditFilter { MaxResults = 5 });

        // Assert
        results.Should().HaveCount(5);
    }

    [Fact]
    public async Task QueryAsync_WithMultipleFilters_ShouldApplyAll()
    {
        // Arrange
        var store = new InMemoryAuditStore(NullLogger<InMemoryAuditStore>.Instance);
        await store.WriteAsync(new AuditEntry 
        { 
            EntityType = "User", 
            Action = "Create", 
            User = "admin@test.com",
            Category = "Security"
        });
        await store.WriteAsync(new AuditEntry 
        { 
            EntityType = "User", 
            Action = "Update", 
            User = "user@test.com",
            Category = "Data"
        });
        await store.WriteAsync(new AuditEntry 
        { 
            EntityType = "Product", 
            Action = "Create", 
            User = "admin@test.com",
            Category = "Security"
        });

        // Act
        var results = await store.QueryAsync(new AuditFilter 
        { 
            EntityType = "User",
            User = "admin@test.com",
            Category = "Security"
        });

        // Assert
        results.Should().HaveCount(1);
        results.First().Action.Should().Be("Create");
    }

    [Fact]
    public async Task GetByEntityAsync_ShouldReturnEntityHistory()
    {
        // Arrange
        var store = new InMemoryAuditStore(NullLogger<InMemoryAuditStore>.Instance);
        await store.WriteAsync(new AuditEntry { EntityType = "User", EntityId = "123", Action = "Create" });
        await store.WriteAsync(new AuditEntry { EntityType = "User", EntityId = "123", Action = "Update" });
        await store.WriteAsync(new AuditEntry { EntityType = "User", EntityId = "456", Action = "Create" });

        // Act
        var results = await store.GetByEntityAsync("User", "123");

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(e => e.EntityId.Should().Be("123"));
    }

    [Fact]
    public async Task GetByEntityAsync_ShouldReturnOrderedByTimestampDescending()
    {
        // Arrange
        var store = new InMemoryAuditStore(NullLogger<InMemoryAuditStore>.Instance);
        await Task.Delay(10); // Ensure different timestamps
        await store.WriteAsync(new AuditEntry 
        { 
            EntityType = "User", 
            EntityId = "123", 
            Action = "Create",
            Timestamp = DateTime.UtcNow.AddHours(-2)
        });
        await Task.Delay(10);
        await store.WriteAsync(new AuditEntry 
        { 
            EntityType = "User", 
            EntityId = "123", 
            Action = "Update",
            Timestamp = DateTime.UtcNow.AddHours(-1)
        });

        // Act
        var results = (await store.GetByEntityAsync("User", "123")).ToList();

        // Assert
        results.Should().HaveCount(2);
        results[0].Action.Should().Be("Update"); // Most recent first
        results[1].Action.Should().Be("Create");
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveAllEntries()
    {
        // Arrange
        var store = new InMemoryAuditStore(NullLogger<InMemoryAuditStore>.Instance);
        await store.WriteAsync(new AuditEntry { Action = "Create" });
        await store.WriteAsync(new AuditEntry { Action = "Update" });

        // Act
        await store.ClearAsync();

        // Assert
        store.Count.Should().Be(0);
        var results = await store.QueryAsync(new AuditFilter());
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task AuditEntry_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var entry = new AuditEntry();

        // Assert
        entry.Id.Should().NotBeEmpty();
        entry.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entry.Category.Should().Be("Data");
        entry.Severity.Should().Be(AuditSeverity.Normal);
        entry.Success.Should().BeTrue();
    }

    [Fact]
    public async Task AuditEntry_WithParameters_ShouldStoreParameters()
    {
        // Arrange
        var store = new InMemoryAuditStore(NullLogger<InMemoryAuditStore>.Instance);
        var entry = new AuditEntry
        {
            Action = "Update",
            EntityType = "User",
            Parameters = new Dictionary<string, object>
            {
                { "email", "user@test.com" },
                { "status", "active" }
            }
        };

        // Act
        await store.WriteAsync(entry);
        var results = await store.QueryAsync(new AuditFilter());

        // Assert
        var retrieved = results.First();
        retrieved.Parameters.Should().NotBeNull();
        retrieved.Parameters!["email"].Should().Be("user@test.com");
        retrieved.Parameters["status"].Should().Be("active");
    }

    [Fact]
    public async Task AuditEntry_WithOldAndNewValues_ShouldStore()
    {
        // Arrange
        var store = new InMemoryAuditStore(NullLogger<InMemoryAuditStore>.Instance);
        var entry = new AuditEntry
        {
            Action = "Update",
            EntityType = "User",
            EntityId = "123",
            OldValue = "{\"name\":\"John\",\"status\":\"inactive\"}",
            NewValue = "{\"name\":\"John\",\"status\":\"active\"}"
        };

        // Act
        await store.WriteAsync(entry);
        var results = await store.GetByEntityAsync("User", "123");

        // Assert
        var retrieved = results.First();
        retrieved.OldValue.Should().Contain("inactive");
        retrieved.NewValue.Should().Contain("active");
    }
}
