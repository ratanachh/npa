using FluentAssertions;
using NPA.Core.Annotations;
using NPA.Core.Core;
using NPA.Core.Metadata;
using NPA.Core.Query;
using Xunit;

namespace NPA.Core.Tests.Cascade;

public class CascadeOperationsTests
{
    [Fact]
    public async Task CascadePersist_WithCascadeAll_ShouldPersistRelatedEntities()
    {
        // Arrange
        var cascadeService = new CascadeService();
        var mockEntityManager = new MockEntityManager();
        var visited = new HashSet<object>();

        var parent = new ParentEntity
        {
            Id = 1,
            Name = "Parent",
            Children = new List<ChildEntity>
            {
                new ChildEntity { Id = 1, Name = "Child1" },
                new ChildEntity { Id = 2, Name = "Child2" }
            }
        };

        var metadata = CreateParentMetadata();

        // Act
        await cascadeService.CascadePersistAsync(parent, metadata, mockEntityManager, visited);

        // Assert
        mockEntityManager.PersistedEntities.Should().HaveCount(2);
        mockEntityManager.PersistedEntities.Should().Contain(e => ((ChildEntity)e).Name == "Child1");
        mockEntityManager.PersistedEntities.Should().Contain(e => ((ChildEntity)e).Name == "Child2");
    }

    [Fact]
    public async Task CascadePersist_WithoutCascade_ShouldNotPersistRelatedEntities()
    {
        // Arrange
        var cascadeService = new CascadeService();
        var mockEntityManager = new MockEntityManager();
        var visited = new HashSet<object>();

        var parent = new ParentEntityNoCascade
        {
            Id = 1,
            Name = "Parent",
            Children = new List<ChildEntity>
            {
                new ChildEntity { Id = 1, Name = "Child1" }
            }
        };

        var metadata = CreateParentNoCascadeMetadata();

        // Act
        await cascadeService.CascadePersistAsync(parent, metadata, mockEntityManager, visited);

        // Assert
        mockEntityManager.PersistedEntities.Should().BeEmpty();
    }

    [Fact]
    public async Task CascadePersist_WithCircularReference_ShouldNotCauseInfiniteLoop()
    {
        // Arrange
        var cascadeService = new CascadeService();
        var mockEntityManager = new MockEntityManager();
        var visited = new HashSet<object>();

        var child = new ChildEntityWithParent { Id = 1, Name = "Child" };
        var parent = new ParentEntityBidirectional
        {
            Id = 1,
            Name = "Parent",
            Children = new List<ChildEntityWithParent> { child }
        };
        child.Parent = parent;

        var metadata = CreateBidirectionalMetadata();

        // Act
        await cascadeService.CascadePersistAsync(parent, metadata, mockEntityManager, visited);

        // Assert - should complete without stack overflow
        mockEntityManager.PersistedEntities.Should().HaveCount(1);
        visited.Should().Contain(parent);
        visited.Should().Contain(child);
    }

    [Fact]
    public async Task CascadeMerge_WithCascadeAll_ShouldMergeRelatedEntities()
    {
        // Arrange
        var cascadeService = new CascadeService();
        var mockEntityManager = new MockEntityManager();
        var visited = new HashSet<object>();

        var parent = new ParentEntity
        {
            Id = 1,
            Name = "Parent",
            Children = new List<ChildEntity>
            {
                new ChildEntity { Id = 1, Name = "UpdatedChild" }
            }
        };

        var metadata = CreateParentMetadata();

        // Act
        await cascadeService.CascadeMergeAsync(parent, metadata, mockEntityManager, visited);

        // Assert
        mockEntityManager.MergedEntities.Should().HaveCount(1);
        mockEntityManager.MergedEntities.Should().Contain(e => ((ChildEntity)e).Name == "UpdatedChild");
    }

    [Fact]
    public async Task CascadeRemove_WithCascadeAll_ShouldRemoveRelatedEntities()
    {
        // Arrange
        var cascadeService = new CascadeService();
        var mockEntityManager = new MockEntityManager();
        var visited = new HashSet<object>();

        var parent = new ParentEntity
        {
            Id = 1,
            Name = "Parent",
            Children = new List<ChildEntity>
            {
                new ChildEntity { Id = 1, Name = "Child1" },
                new ChildEntity { Id = 2, Name = "Child2" }
            }
        };

        var metadata = CreateParentMetadata();

        // Act
        await cascadeService.CascadeRemoveAsync(parent, metadata, mockEntityManager, visited);

        // Assert
        mockEntityManager.RemovedEntities.Should().HaveCount(2);
        mockEntityManager.RemovedEntities.Should().Contain(e => ((ChildEntity)e).Name == "Child1");
        mockEntityManager.RemovedEntities.Should().Contain(e => ((ChildEntity)e).Name == "Child2");
    }

    [Fact]
    public async Task CascadeRemove_WithOrphanRemoval_ShouldRemoveOrphans()
    {
        // Arrange
        var cascadeService = new CascadeService();
        var mockEntityManager = new MockEntityManager();
        var visited = new HashSet<object>();

        var parent = new ParentEntityOrphanRemoval
        {
            Id = 1,
            Name = "Parent",
            Children = new List<ChildEntity>
            {
                new ChildEntity { Id = 1, Name = "Orphan" }
            }
        };

        var metadata = CreateOrphanRemovalMetadata();

        // Act
        await cascadeService.CascadeRemoveAsync(parent, metadata, mockEntityManager, visited);

        // Assert
        mockEntityManager.RemovedEntities.Should().HaveCount(1);
        mockEntityManager.RemovedEntities.Should().Contain(e => ((ChildEntity)e).Name == "Orphan");
    }

    [Fact]
    public async Task CascadeDetach_WithCascadeAll_ShouldDetachRelatedEntities()
    {
        // Arrange
        var cascadeService = new CascadeService();
        var mockEntityManager = new MockEntityManager();
        var visited = new HashSet<object>();

        var parent = new ParentEntity
        {
            Id = 1,
            Name = "Parent",
            Children = new List<ChildEntity>
            {
                new ChildEntity { Id = 1, Name = "Child1" }
            }
        };

        var metadata = CreateParentMetadata();

        // Act
        await cascadeService.CascadeDetachAsync(parent, metadata, mockEntityManager, visited);

        // Assert
        mockEntityManager.DetachedEntities.Should().HaveCount(1);
        mockEntityManager.DetachedEntities.Should().Contain(e => ((ChildEntity)e).Name == "Child1");
    }

    [Fact]
    public async Task CascadePersist_WithSingleRelation_ShouldPersistSingleEntity()
    {
        // Arrange
        var cascadeService = new CascadeService();
        var mockEntityManager = new MockEntityManager();
        var visited = new HashSet<object>();

        var entity = new EntityWithSingleRelation
        {
            Id = 1,
            Name = "Entity",
            RelatedEntity = new RelatedEntity { Id = 1, Name = "Related" }
        };

        var metadata = CreateSingleRelationMetadata();

        // Act
        await cascadeService.CascadePersistAsync(entity, metadata, mockEntityManager, visited);

        // Assert
        mockEntityManager.PersistedEntities.Should().HaveCount(1);
        mockEntityManager.PersistedEntities.Should().Contain(e => ((RelatedEntity)e).Name == "Related");
    }

    [Fact]
    public async Task CascadePersist_WithNullCollection_ShouldNotThrow()
    {
        // Arrange
        var cascadeService = new CascadeService();
        var mockEntityManager = new MockEntityManager();
        var visited = new HashSet<object>();

        var parent = new ParentEntity
        {
            Id = 1,
            Name = "Parent",
            Children = null! // Null collection
        };

        var metadata = CreateParentMetadata();

        // Act
        Func<Task> act = async () => await cascadeService.CascadePersistAsync(parent, metadata, mockEntityManager, visited);

        // Assert
        await act.Should().NotThrowAsync();
        mockEntityManager.PersistedEntities.Should().BeEmpty();
    }

    [Fact]
    public async Task CascadePersist_WithNullSingleRelation_ShouldNotThrow()
    {
        // Arrange
        var cascadeService = new CascadeService();
        var mockEntityManager = new MockEntityManager();
        var visited = new HashSet<object>();

        var entity = new EntityWithSingleRelation
        {
            Id = 1,
            Name = "Entity",
            RelatedEntity = null // Null relation
        };

        var metadata = CreateSingleRelationMetadata();

        // Act
        Func<Task> act = async () => await cascadeService.CascadePersistAsync(entity, metadata, mockEntityManager, visited);

        // Assert
        await act.Should().NotThrowAsync();
        mockEntityManager.PersistedEntities.Should().BeEmpty();
    }

    // Helper methods to create metadata
    private EntityMetadata CreateParentMetadata()
    {
        return new EntityMetadata
        {
            EntityType = typeof(ParentEntity),
            TableName = "parents",
            PrimaryKeyProperty = "Id",
            Properties = new Dictionary<string, PropertyMetadata>
            {
                { "Id", new PropertyMetadata { PropertyName = "Id", IsPrimaryKey = true } },
                { "Name", new PropertyMetadata { PropertyName = "Name" } }
            },
            Relationships = new Dictionary<string, RelationshipMetadata>
            {
                {
                    "Children",
                    new RelationshipMetadata
                    {
                        PropertyName = "Children",
                        RelationshipType = RelationshipType.OneToMany,
                        TargetEntityType = typeof(ChildEntity),
                        CascadeType = CascadeType.All
                    }
                }
            }
        };
    }

    private EntityMetadata CreateParentNoCascadeMetadata()
    {
        return new EntityMetadata
        {
            EntityType = typeof(ParentEntityNoCascade),
            TableName = "parents",
            PrimaryKeyProperty = "Id",
            Properties = new Dictionary<string, PropertyMetadata>
            {
                { "Id", new PropertyMetadata { PropertyName = "Id", IsPrimaryKey = true } },
                { "Name", new PropertyMetadata { PropertyName = "Name" } }
            },
            Relationships = new Dictionary<string, RelationshipMetadata>
            {
                {
                    "Children",
                    new RelationshipMetadata
                    {
                        PropertyName = "Children",
                        RelationshipType = RelationshipType.OneToMany,
                        TargetEntityType = typeof(ChildEntity),
                        CascadeType = CascadeType.None
                    }
                }
            }
        };
    }

    private EntityMetadata CreateBidirectionalMetadata()
    {
        return new EntityMetadata
        {
            EntityType = typeof(ParentEntityBidirectional),
            TableName = "parents",
            PrimaryKeyProperty = "Id",
            Properties = new Dictionary<string, PropertyMetadata>
            {
                { "Id", new PropertyMetadata { PropertyName = "Id", IsPrimaryKey = true } },
                { "Name", new PropertyMetadata { PropertyName = "Name" } }
            },
            Relationships = new Dictionary<string, RelationshipMetadata>
            {
                {
                    "Children",
                    new RelationshipMetadata
                    {
                        PropertyName = "Children",
                        RelationshipType = RelationshipType.OneToMany,
                        TargetEntityType = typeof(ChildEntityWithParent),
                        CascadeType = CascadeType.All
                    }
                }
            }
        };
    }

    private EntityMetadata CreateOrphanRemovalMetadata()
    {
        return new EntityMetadata
        {
            EntityType = typeof(ParentEntityOrphanRemoval),
            TableName = "parents",
            PrimaryKeyProperty = "Id",
            Properties = new Dictionary<string, PropertyMetadata>
            {
                { "Id", new PropertyMetadata { PropertyName = "Id", IsPrimaryKey = true } },
                { "Name", new PropertyMetadata { PropertyName = "Name" } }
            },
            Relationships = new Dictionary<string, RelationshipMetadata>
            {
                {
                    "Children",
                    new RelationshipMetadata
                    {
                        PropertyName = "Children",
                        RelationshipType = RelationshipType.OneToMany,
                        TargetEntityType = typeof(ChildEntity),
                        CascadeType = CascadeType.None,
                        OrphanRemoval = true
                    }
                }
            }
        };
    }

    private EntityMetadata CreateSingleRelationMetadata()
    {
        return new EntityMetadata
        {
            EntityType = typeof(EntityWithSingleRelation),
            TableName = "entities",
            PrimaryKeyProperty = "Id",
            Properties = new Dictionary<string, PropertyMetadata>
            {
                { "Id", new PropertyMetadata { PropertyName = "Id", IsPrimaryKey = true } },
                { "Name", new PropertyMetadata { PropertyName = "Name" } }
            },
            Relationships = new Dictionary<string, RelationshipMetadata>
            {
                {
                    "RelatedEntity",
                    new RelationshipMetadata
                    {
                        PropertyName = "RelatedEntity",
                        RelationshipType = RelationshipType.ManyToOne,
                        TargetEntityType = typeof(RelatedEntity),
                        CascadeType = CascadeType.All
                    }
                }
            }
        };
    }

    // Test entity classes
    private class ParentEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ChildEntity> Children { get; set; } = new();
    }

    private class ParentEntityNoCascade
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ChildEntity> Children { get; set; } = new();
    }

    private class ParentEntityBidirectional
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ChildEntityWithParent> Children { get; set; } = new();
    }

    private class ParentEntityOrphanRemoval
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ChildEntity> Children { get; set; } = new();
    }

    private class ChildEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class ChildEntityWithParent
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ParentEntityBidirectional? Parent { get; set; }
    }

    private class EntityWithSingleRelation
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public RelatedEntity? RelatedEntity { get; set; }
    }

    private class RelatedEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // Mock EntityManager for testing
    private class MockEntityManager : IEntityManager
    {
        public List<object> PersistedEntities { get; } = new();
        public List<object> MergedEntities { get; } = new();
        public List<object> RemovedEntities { get; } = new();
        public List<object> DetachedEntities { get; } = new();

        public IMetadataProvider MetadataProvider => new MockMetadataProvider();
        public IChangeTracker ChangeTracker => new MockChangeTracker(this);
        public bool HasActiveTransaction => false;

        public Task PersistAsync<T>(T entity) where T : class
        {
            PersistedEntities.Add(entity);
            return Task.CompletedTask;
        }

        public void Persist<T>(T entity) where T : class
        {
            PersistedEntities.Add(entity);
        }

        public Task MergeAsync<T>(T entity) where T : class
        {
            MergedEntities.Add(entity);
            return Task.CompletedTask;
        }

        public void Merge<T>(T entity) where T : class
        {
            MergedEntities.Add(entity);
        }

        public Task RemoveAsync<T>(T entity) where T : class
        {
            RemovedEntities.Add(entity);
            return Task.CompletedTask;
        }

        public void Remove<T>(T entity) where T : class
        {
            RemovedEntities.Add(entity);
        }

        public void Detach<T>(T entity) where T : class
        {
            DetachedEntities.Add(entity);
        }

        // Required interface implementations (not used in cascade tests)
        public Task<T?> FindAsync<T>(object id) where T : class => Task.FromResult<T?>(null);
        public T? Find<T>(object id) where T : class => null;
        public Task<T?> FindAsync<T>(CompositeKey key) where T : class => Task.FromResult<T?>(null);
        public T? Find<T>(CompositeKey key) where T : class => null;
        public Task RemoveAsync<T>(object id) where T : class => Task.CompletedTask;
        public void Remove<T>(object id) where T : class { }
        public Task RemoveAsync<T>(CompositeKey key) where T : class => Task.CompletedTask;
        public void Remove<T>(CompositeKey key) where T : class { }
        public Task FlushAsync() => Task.CompletedTask;
        public void Flush() { }
        public Task ClearAsync() => Task.CompletedTask;
        public void Clear() { }
        public bool Contains<T>(T entity) where T : class => false;
        public IQuery<T> CreateQuery<T>(string cpql) where T : class => null!;
        public Task<ITransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted) => Task.FromResult<ITransaction>(null!);
        public ITransaction BeginTransaction(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted) => null!;
        public ITransaction? GetCurrentTransaction() => null;
        public void Dispose() { }
    }

    private class MockMetadataProvider : IMetadataProvider
    {
        public EntityMetadata GetEntityMetadata<T>() => new EntityMetadata { EntityType = typeof(T) };
        public EntityMetadata GetEntityMetadata(Type entityType) => new EntityMetadata { EntityType = entityType };
        public EntityMetadata GetEntityMetadata(string entityName) => new EntityMetadata();
        public bool IsEntity(Type type) => true;
    }

    private class MockChangeTracker : IChangeTracker
    {
        private readonly MockEntityManager _entityManager;

        public MockChangeTracker(MockEntityManager entityManager)
        {
            _entityManager = entityManager;
        }

        public void Untrack<T>(T entity) where T : class
        {
            _entityManager.DetachedEntities.Add(entity);
        }

        // Required interface implementations (not used in cascade tests)
        public void Track<T>(T entity, EntityState state) where T : class { }
        public EntityState GetState<T>(T entity) where T : class => EntityState.Detached;
        public void SetState<T>(T entity, EntityState state) where T : class { }
        public IEnumerable<object> GetTrackedEntities(EntityState state) => Enumerable.Empty<object>();
        public void Clear() { }
        public IReadOnlyDictionary<object, EntityState> GetPendingChanges() => new Dictionary<object, EntityState>();
        public bool IsModified(object entity) => false;
        public T? GetTrackedEntityById<T>(object id) where T : class => null;
        public bool HasChanges(object entity) => false;
        public void CopyEntityValues(object source, object target) { }
        public void QueueOperation(object entity, EntityState state, Func<string> sqlGenerator, object parameters) { }
        public IEnumerable<QueuedOperation> GetQueuedOperations() => Enumerable.Empty<QueuedOperation>();
        public void ClearQueue() { }
        public int GetQueuedOperationCount() => 0;
    }
}
