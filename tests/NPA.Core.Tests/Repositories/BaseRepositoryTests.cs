using System.Data;
using FluentAssertions;
using Moq;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Core.Repositories;
using NPA.Core.Tests.TestEntities;
using Xunit;

namespace NPA.Core.Tests.Repositories;

/// <summary>
/// Tests for BaseRepository.
/// </summary>
public class BaseRepositoryTests
{
    private readonly Mock<IDbConnection> _connectionMock;
    private readonly Mock<IEntityManager> _entityManagerMock;
    private readonly IMetadataProvider _metadataProvider;
    private readonly BaseRepository<User, long> _repository;
    
    public BaseRepositoryTests()
    {
        _connectionMock = new Mock<IDbConnection>();
        _entityManagerMock = new Mock<IEntityManager>();
        _metadataProvider = new MetadataProvider();
        _repository = new BaseRepository<User, long>(_connectionMock.Object, _entityManagerMock.Object, _metadataProvider);
    }
    
    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenConnectionIsNull()
    {
        // Act & Assert
        Action act = () => new BaseRepository<User, long>(null!, _entityManagerMock.Object, _metadataProvider);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connection");
    }
    
    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenEntityManagerIsNull()
    {
        // Act & Assert
        Action act = () => new BaseRepository<User, long>(_connectionMock.Object, null!, _metadataProvider);
        act.Should().Throw<ArgumentNullException>().WithParameterName("entityManager");
    }
    
    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenMetadataProviderIsNull()
    {
        // Act & Assert
        Action act = () => new BaseRepository<User, long>(_connectionMock.Object, _entityManagerMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("metadataProvider");
    }
    
    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity_WhenEntityExists()
    {
        // Arrange
        var user = new User { Id = 1, Username = "test_user", Email = "test@example.com" };
        _entityManagerMock.Setup(m => m.FindAsync<User>(1L))
            .ReturnsAsync(user);
        
        // Act
        var result = await _repository.GetByIdAsync(1L);
        
        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Username.Should().Be("test_user");
    }
    
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenEntityDoesNotExist()
    {
        // Arrange
        _entityManagerMock.Setup(m => m.FindAsync<User>(1L))
            .ReturnsAsync((User?)null);
        
        // Act
        var result = await _repository.GetByIdAsync(1L);
        
        // Assert
        result.Should().BeNull();
    }
    
    // Note: Skipping null ID test because long is a non-nullable value type
    
    [Fact]
    public async Task AddAsync_ShouldCallPersistAsync()
    {
        // Arrange
        var user = new User { Username = "new_user", Email = "new@example.com" };
        
        // Act
        var result = await _repository.AddAsync(user);
        
        // Assert
        result.Should().Be(user);
        _entityManagerMock.Verify(m => m.PersistAsync(user), Times.Once);
    }
    
    [Fact]
    public async Task AddAsync_ShouldThrowArgumentNullException_WhenEntityIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddAsync(null!));
    }
    
    [Fact]
    public async Task UpdateAsync_ShouldCallMergeAsync()
    {
        // Arrange
        var user = new User { Id = 1, Username = "updated_user", Email = "updated@example.com" };
        
        // Act
        await _repository.UpdateAsync(user);
        
        // Assert
        _entityManagerMock.Verify(m => m.MergeAsync(user), Times.Once);
    }
    
    [Fact]
    public async Task UpdateAsync_ShouldThrowArgumentNullException_WhenEntityIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.UpdateAsync(null!));
    }
    
    [Fact]
    public async Task DeleteAsync_WithId_ShouldCallRemoveAsync()
    {
        // Act
        await _repository.DeleteAsync(1L);
        
        // Assert
        _entityManagerMock.Verify(m => m.RemoveAsync<User>(1L), Times.Once);
    }
    
    // Note: Skipping null ID test because long is a non-nullable value type
    
    [Fact]
    public async Task DeleteAsync_WithEntity_ShouldCallRemoveAsync()
    {
        // Arrange
        var user = new User { Id = 1, Username = "delete_user", Email = "delete@example.com" };
        
        // Act
        await _repository.DeleteAsync(user);
        
        // Assert
        _entityManagerMock.Verify(m => m.RemoveAsync(user), Times.Once);
    }
    
    [Fact]
    public async Task DeleteAsync_WithEntity_ShouldThrowArgumentNullException_WhenEntityIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.DeleteAsync((User)null!));
    }
    
    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenEntityExists()
    {
        // Arrange
        var user = new User { Id = 1, Username = "existing_user", Email = "existing@example.com" };
        _entityManagerMock.Setup(m => m.FindAsync<User>(1L))
            .ReturnsAsync(user);
        
        // Act
        var result = await _repository.ExistsAsync(1L);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenEntityDoesNotExist()
    {
        // Arrange
        _entityManagerMock.Setup(m => m.FindAsync<User>(1L))
            .ReturnsAsync((User?)null);
        
        // Act
        var result = await _repository.ExistsAsync(1L);
        
        // Assert
        result.Should().BeFalse();
    }
}

