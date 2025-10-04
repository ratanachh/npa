using FluentAssertions;
using NPA.Core.Core;
using NPA.Core.Tests.TestEntities;
using Xunit;

namespace NPA.Core.Tests.Core;

/// <summary>
/// Unit tests for the ChangeTracker class.
/// </summary>
public class ChangeTrackerTests
{
    private readonly ChangeTracker _changeTracker;

    public ChangeTrackerTests()
    {
        _changeTracker = new ChangeTracker();
    }

    [Fact]
    public void Track_WithValidEntity_ShouldTrackEntity()
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser" };

        // Act
        _changeTracker.Track(user, EntityState.Added);

        // Assert
        var state = _changeTracker.GetState(user);
        state.Should().Be(EntityState.Added);
    }

    [Fact]
    public void Track_WithNullEntity_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _changeTracker.Track<User>(null!, EntityState.Added);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetState_WithTrackedEntity_ShouldUpdateState()
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser" };
        _changeTracker.Track(user, EntityState.Added);

        // Act
        _changeTracker.SetState(user, EntityState.Modified);

        // Assert
        var state = _changeTracker.GetState(user);
        state.Should().Be(EntityState.Modified);
    }

    [Fact]
    public void SetState_WithUntrackedEntity_ShouldTrackEntity()
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser" };

        // Act
        _changeTracker.SetState(user, EntityState.Deleted);

        // Assert
        var state = _changeTracker.GetState(user);
        state.Should().Be(EntityState.Deleted);
    }

    [Fact]
    public void SetState_WithNullEntity_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _changeTracker.SetState<User>(null!, EntityState.Modified);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetState_WithTrackedEntity_ShouldReturnState()
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser" };
        _changeTracker.Track(user, EntityState.Unchanged);

        // Act
        var state = _changeTracker.GetState(user);

        // Assert
        state.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public void GetState_WithUntrackedEntity_ShouldReturnNull()
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser" };

        // Act
        var state = _changeTracker.GetState(user);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void GetState_WithNullEntity_ShouldReturnNull()
    {
        // Act
        var state = _changeTracker.GetState<User>(null!);

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public void Untrack_WithTrackedEntity_ShouldRemoveEntity()
    {
        // Arrange
        var user = new User { Id = 1, Username = "testuser" };
        _changeTracker.Track(user, EntityState.Added);

        // Act
        _changeTracker.Untrack(user);

        // Assert
        var state = _changeTracker.GetState(user);
        state.Should().BeNull();
    }

    [Fact]
    public void Untrack_WithNullEntity_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => _changeTracker.Untrack<User>(null!);
        action.Should().NotThrow();
    }

    [Fact]
    public void GetTrackedEntities_WithSpecificState_ShouldReturnMatchingEntities()
    {
        // Arrange
        var user1 = new User { Id = 1, Username = "user1" };
        var user2 = new User { Id = 2, Username = "user2" };
        var user3 = new User { Id = 3, Username = "user3" };

        _changeTracker.Track(user1, EntityState.Added);
        _changeTracker.Track(user2, EntityState.Modified);
        _changeTracker.Track(user3, EntityState.Added);

        // Act
        var addedEntities = _changeTracker.GetTrackedEntities(EntityState.Added);

        // Assert
        addedEntities.Should().HaveCount(2);
        addedEntities.Should().Contain(user1);
        addedEntities.Should().Contain(user3);
    }

    [Fact]
    public void GetPendingChanges_ShouldReturnNonUnchangedEntities()
    {
        // Arrange
        var user1 = new User { Id = 1, Username = "user1" };
        var user2 = new User { Id = 2, Username = "user2" };
        var user3 = new User { Id = 3, Username = "user3" };

        _changeTracker.Track(user1, EntityState.Added);
        _changeTracker.Track(user2, EntityState.Unchanged);
        _changeTracker.Track(user3, EntityState.Deleted);

        // Act
        var pendingChanges = _changeTracker.GetPendingChanges();

        // Assert
        pendingChanges.Should().HaveCount(2);
        pendingChanges[user1].Should().Be(EntityState.Added);
        pendingChanges[user3].Should().Be(EntityState.Deleted);
    }

    [Fact]
    public void Clear_ShouldRemoveAllTrackedEntities()
    {
        // Arrange
        var user1 = new User { Id = 1, Username = "user1" };
        var user2 = new User { Id = 2, Username = "user2" };

        _changeTracker.Track(user1, EntityState.Added);
        _changeTracker.Track(user2, EntityState.Modified);

        // Act
        _changeTracker.Clear();

        // Assert
        _changeTracker.GetState(user1).Should().BeNull();
        _changeTracker.GetState(user2).Should().BeNull();
        _changeTracker.GetPendingChanges().Should().BeEmpty();
    }
}
