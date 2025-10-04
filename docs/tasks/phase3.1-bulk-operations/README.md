# Phase 3.3: Bulk Operations

## 📋 Task Overview

**Objective**: Implement efficient bulk operations for insert, update, and delete operations that leverage Dapper's performance while providing a clean API.

**Priority**: Medium  
**Estimated Time**: 3-4 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.6, Phase 3.1-3.2 (Transaction Management, Cascade Operations)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] IBulkOperations interface is complete
- [ ] Bulk insert operations work
- [ ] Bulk update operations work
- [ ] Bulk delete operations work
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## 📝 Detailed Requirements

### 1. IBulkOperations Interface
- **Purpose**: Defines the contract for bulk operations
- **Methods**:
  - `Task<int> BulkInsertAsync<T>(IEnumerable<T> entities)` - Bulk insert entities
  - `Task<int> BulkUpdateAsync<T>(IEnumerable<T> entities)` - Bulk update entities
  - `Task<int> BulkDeleteAsync<T>(IEnumerable<T> entities)` - Bulk delete entities
  - `Task<int> BulkUpsertAsync<T>(IEnumerable<T> entities)` - Bulk upsert entities
  - `Task<BulkResult<T>> BulkInsertWithResultAsync<T>(IEnumerable<T> entities)` - Bulk insert with result
  - `Task<BulkResult<T>> BulkUpdateWithResultAsync<T>(IEnumerable<T> entities)` - Bulk update with result

### 2. Bulk Insert Operations
- **Batch Processing**: Process entities in batches
- **Identity Handling**: Handle identity columns
- **Error Handling**: Handle batch errors
- **Performance Optimization**: Optimize insert performance

### 3. Bulk Update Operations
- **Batch Processing**: Process entities in batches
- **Key Matching**: Match entities by primary key
- **Change Detection**: Detect changed properties
- **Performance Optimization**: Optimize update performance

### 4. Bulk Delete Operations
- **Batch Processing**: Process entities in batches
- **Key Extraction**: Extract primary keys
- **Cascade Handling**: Handle cascade deletes
- **Performance Optimization**: Optimize delete performance

### 5. Bulk Upsert Operations
- **Merge Logic**: Implement merge logic
- **Conflict Resolution**: Handle conflicts
- **Performance Optimization**: Optimize upsert performance

## 🏗️ Implementation Plan

### Step 1: Create Bulk Operations Interface
1. Create `IBulkOperations` interface
2. Create `BulkResult<T>` class
3. Create `BulkOptions` class
4. Create `BulkException` class

### Step 2: Implement Bulk Operations
1. Create `BulkOperations` class
2. Implement bulk insert
3. Implement bulk update
4. Implement bulk delete
5. Implement bulk upsert

### Step 3: Add Batch Processing
1. Create `IBatchProcessor` interface
2. Create `BatchProcessor` class
3. Implement batch processing
4. Add batch size configuration

### Step 4: Add Performance Optimization
1. Implement connection pooling
2. Implement parallel processing
3. Implement memory optimization
4. Add performance monitoring

### Step 5: Add Error Handling
1. Implement error collection
2. Implement partial success handling
3. Implement retry logic
4. Add error reporting

### Step 6: Create Unit Tests
1. Test bulk operations
2. Test batch processing
3. Test performance optimization
4. Test error handling

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. Bulk operations guide
4. Best practices

## 📁 File Structure

```
src/NPA.Core/BulkOperations/
├── IBulkOperations.cs
├── BulkOperations.cs
├── BulkResult.cs
├── BulkOptions.cs
├── BulkException.cs
├── IBatchProcessor.cs
├── BatchProcessor.cs
├── IBulkInsertStrategy.cs
├── BulkInsertStrategy.cs
├── IBulkUpdateStrategy.cs
├── BulkUpdateStrategy.cs
├── IBulkDeleteStrategy.cs
├── BulkDeleteStrategy.cs
└── IBulkUpsertStrategy.cs
    └── BulkUpsertStrategy.cs

tests/NPA.Core.Tests/BulkOperations/
├── BulkOperationsTests.cs
├── BatchProcessorTests.cs
├── BulkInsertStrategyTests.cs
├── BulkUpdateStrategyTests.cs
├── BulkDeleteStrategyTests.cs
└── BulkUpsertStrategyTests.cs
```

## 💻 Code Examples

### IBulkOperations Interface
```csharp
public interface IBulkOperations
{
    Task<int> BulkInsertAsync<T>(IEnumerable<T> entities) where T : class;
    Task<int> BulkInsertAsync<T>(IEnumerable<T> entities, BulkOptions options) where T : class;
    Task<BulkResult<T>> BulkInsertWithResultAsync<T>(IEnumerable<T> entities) where T : class;
    Task<BulkResult<T>> BulkInsertWithResultAsync<T>(IEnumerable<T> entities, BulkOptions options) where T : class;
    
    Task<int> BulkUpdateAsync<T>(IEnumerable<T> entities) where T : class;
    Task<int> BulkUpdateAsync<T>(IEnumerable<T> entities, BulkOptions options) where T : class;
    Task<BulkResult<T>> BulkUpdateWithResultAsync<T>(IEnumerable<T> entities) where T : class;
    Task<BulkResult<T>> BulkUpdateWithResultAsync<T>(IEnumerable<T> entities, BulkOptions options) where T : class;
    
    Task<int> BulkDeleteAsync<T>(IEnumerable<T> entities) where T : class;
    Task<int> BulkDeleteAsync<T>(IEnumerable<T> entities, BulkOptions options) where T : class;
    Task<BulkResult<T>> BulkDeleteWithResultAsync<T>(IEnumerable<T> entities) where T : class;
    Task<BulkResult<T>> BulkDeleteWithResultAsync<T>(IEnumerable<T> entities, BulkOptions options) where T : class;
    
    Task<int> BulkUpsertAsync<T>(IEnumerable<T> entities) where T : class;
    Task<int> BulkUpsertAsync<T>(IEnumerable<T> entities, BulkOptions options) where T : class;
    Task<BulkResult<T>> BulkUpsertWithResultAsync<T>(IEnumerable<T> entities) where T : class;
    Task<BulkResult<T>> BulkUpsertWithResultAsync<T>(IEnumerable<T> entities, BulkOptions options) where T : class;
}

public class BulkResult<T>
{
    public int AffectedRows { get; set; }
    public List<T> ProcessedEntities { get; set; } = new();
    public List<BulkError> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public bool Success => !Errors.Any();
    public int ErrorCount => Errors.Count;
    public int ProcessedCount => ProcessedEntities.Count;
}

public class BulkError
{
    public int Index { get; set; }
    public object Entity { get; set; }
    public string Message { get; set; }
    public Exception Exception { get; set; }
    public string PropertyName { get; set; }
    public object PropertyValue { get; set; }
}

public class BulkOptions
{
    public int BatchSize { get; set; } = 1000;
    public int CommandTimeout { get; set; } = 30;
    public bool UseTransaction { get; set; } = true;
    public bool StopOnError { get; set; } = false;
    public bool ReturnAffectedRows { get; set; } = true;
    public bool ValidateEntities { get; set; } = true;
    public bool IgnoreDuplicateKeys { get; set; } = false;
    public bool UpdateOnDuplicate { get; set; } = false;
    public int MaxRetries { get; set; } = 0;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
}
```

### BulkOperations Class
```csharp
public class BulkOperations : IBulkOperations
{
    private readonly IDbConnection _connection;
    private readonly IMetadataProvider _metadataProvider;
    private readonly IBatchProcessor _batchProcessor;
    private readonly IBulkInsertStrategy _insertStrategy;
    private readonly IBulkUpdateStrategy _updateStrategy;
    private readonly IBulkDeleteStrategy _deleteStrategy;
    private readonly IBulkUpsertStrategy _upsertStrategy;
    
    public BulkOperations(
        IDbConnection connection,
        IMetadataProvider metadataProvider,
        IBatchProcessor batchProcessor,
        IBulkInsertStrategy insertStrategy,
        IBulkUpdateStrategy updateStrategy,
        IBulkDeleteStrategy deleteStrategy,
        IBulkUpsertStrategy upsertStrategy)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
        _batchProcessor = batchProcessor ?? throw new ArgumentNullException(nameof(batchProcessor));
        _insertStrategy = insertStrategy ?? throw new ArgumentNullException(nameof(insertStrategy));
        _updateStrategy = updateStrategy ?? throw new ArgumentNullException(nameof(updateStrategy));
        _deleteStrategy = deleteStrategy ?? throw new ArgumentNullException(nameof(deleteStrategy));
        _upsertStrategy = upsertStrategy ?? throw new ArgumentNullException(nameof(upsertStrategy));
    }
    
    public async Task<int> BulkInsertAsync<T>(IEnumerable<T> entities) where T : class
    {
        var options = new BulkOptions();
        return await BulkInsertAsync(entities, options);
    }
    
    public async Task<int> BulkInsertAsync<T>(IEnumerable<T> entities, BulkOptions options) where T : class
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (options == null) throw new ArgumentNullException(nameof(options));
        
        var result = await BulkInsertWithResultAsync(entities, options);
        return result.AffectedRows;
    }
    
    public async Task<BulkResult<T>> BulkInsertWithResultAsync<T>(IEnumerable<T> entities) where T : class
    {
        var options = new BulkOptions();
        return await BulkInsertWithResultAsync(entities, options);
    }
    
    public async Task<BulkResult<T>> BulkInsertWithResultAsync<T>(IEnumerable<T> entities, BulkOptions options) where T : class
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (options == null) throw new ArgumentNullException(nameof(options));
        
        var result = new BulkResult<T>();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var entityList = entities.ToList();
            if (!entityList.Any())
            {
                return result;
            }
            
            var metadata = _metadataProvider.GetEntityMetadata<T>();
            var batches = _batchProcessor.CreateBatches(entityList, options.BatchSize);
            
            foreach (var batch in batches)
            {
                var batchResult = await _insertStrategy.ExecuteBatchAsync(batch, metadata, options);
                result.AffectedRows += batchResult.AffectedRows;
                result.ProcessedEntities.AddRange(batchResult.ProcessedEntities);
                result.Errors.AddRange(batchResult.Errors);
                
                if (options.StopOnError && batchResult.Errors.Any())
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add(new BulkError
            {
                Index = -1,
                Entity = null,
                Message = ex.Message,
                Exception = ex
            });
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }
        
        return result;
    }
    
    public async Task<int> BulkUpdateAsync<T>(IEnumerable<T> entities) where T : class
    {
        var options = new BulkOptions();
        return await BulkUpdateAsync(entities, options);
    }
    
    public async Task<int> BulkUpdateAsync<T>(IEnumerable<T> entities, BulkOptions options) where T : class
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (options == null) throw new ArgumentNullException(nameof(options));
        
        var result = await BulkUpdateWithResultAsync(entities, options);
        return result.AffectedRows;
    }
    
    public async Task<BulkResult<T>> BulkUpdateWithResultAsync<T>(IEnumerable<T> entities) where T : class
    {
        var options = new BulkOptions();
        return await BulkUpdateWithResultAsync(entities, options);
    }
    
    public async Task<BulkResult<T>> BulkUpdateWithResultAsync<T>(IEnumerable<T> entities, BulkOptions options) where T : class
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (options == null) throw new ArgumentNullException(nameof(options));
        
        var result = new BulkResult<T>();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var entityList = entities.ToList();
            if (!entityList.Any())
            {
                return result;
            }
            
            var metadata = _metadataProvider.GetEntityMetadata<T>();
            var batches = _batchProcessor.CreateBatches(entityList, options.BatchSize);
            
            foreach (var batch in batches)
            {
                var batchResult = await _updateStrategy.ExecuteBatchAsync(batch, metadata, options);
                result.AffectedRows += batchResult.AffectedRows;
                result.ProcessedEntities.AddRange(batchResult.ProcessedEntities);
                result.Errors.AddRange(batchResult.Errors);
                
                if (options.StopOnError && batchResult.Errors.Any())
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add(new BulkError
            {
                Index = -1,
                Entity = null,
                Message = ex.Message,
                Exception = ex
            });
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }
        
        return result;
    }
    
    public async Task<int> BulkDeleteAsync<T>(IEnumerable<T> entities) where T : class
    {
        var options = new BulkOptions();
        return await BulkDeleteAsync(entities, options);
    }
    
    public async Task<int> BulkDeleteAsync<T>(IEnumerable<T> entities, BulkOptions options) where T : class
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (options == null) throw new ArgumentNullException(nameof(options));
        
        var result = await BulkDeleteWithResultAsync(entities, options);
        return result.AffectedRows;
    }
    
    public async Task<BulkResult<T>> BulkDeleteWithResultAsync<T>(IEnumerable<T> entities) where T : class
    {
        var options = new BulkOptions();
        return await BulkDeleteWithResultAsync(entities, options);
    }
    
    public async Task<BulkResult<T>> BulkDeleteWithResultAsync<T>(IEnumerable<T> entities, BulkOptions options) where T : class
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (options == null) throw new ArgumentNullException(nameof(options));
        
        var result = new BulkResult<T>();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var entityList = entities.ToList();
            if (!entityList.Any())
            {
                return result;
            }
            
            var metadata = _metadataProvider.GetEntityMetadata<T>();
            var batches = _batchProcessor.CreateBatches(entityList, options.BatchSize);
            
            foreach (var batch in batches)
            {
                var batchResult = await _deleteStrategy.ExecuteBatchAsync(batch, metadata, options);
                result.AffectedRows += batchResult.AffectedRows;
                result.ProcessedEntities.AddRange(batchResult.ProcessedEntities);
                result.Errors.AddRange(batchResult.Errors);
                
                if (options.StopOnError && batchResult.Errors.Any())
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add(new BulkError
            {
                Index = -1,
                Entity = null,
                Message = ex.Message,
                Exception = ex
            });
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }
        
        return result;
    }
    
    public async Task<int> BulkUpsertAsync<T>(IEnumerable<T> entities) where T : class
    {
        var options = new BulkOptions();
        return await BulkUpsertAsync(entities, options);
    }
    
    public async Task<int> BulkUpsertAsync<T>(IEnumerable<T> entities, BulkOptions options) where T : class
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (options == null) throw new ArgumentNullException(nameof(options));
        
        var result = await BulkUpsertWithResultAsync(entities, options);
        return result.AffectedRows;
    }
    
    public async Task<BulkResult<T>> BulkUpsertWithResultAsync<T>(IEnumerable<T> entities) where T : class
    {
        var options = new BulkOptions();
        return await BulkUpsertWithResultAsync(entities, options);
    }
    
    public async Task<BulkResult<T>> BulkUpsertWithResultAsync<T>(IEnumerable<T> entities, BulkOptions options) where T : class
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));
        if (options == null) throw new ArgumentNullException(nameof(options));
        
        var result = new BulkResult<T>();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var entityList = entities.ToList();
            if (!entityList.Any())
            {
                return result;
            }
            
            var metadata = _metadataProvider.GetEntityMetadata<T>();
            var batches = _batchProcessor.CreateBatches(entityList, options.BatchSize);
            
            foreach (var batch in batches)
            {
                var batchResult = await _upsertStrategy.ExecuteBatchAsync(batch, metadata, options);
                result.AffectedRows += batchResult.AffectedRows;
                result.ProcessedEntities.AddRange(batchResult.ProcessedEntities);
                result.Errors.AddRange(batchResult.Errors);
                
                if (options.StopOnError && batchResult.Errors.Any())
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add(new BulkError
            {
                Index = -1,
                Entity = null,
                Message = ex.Message,
                Exception = ex
            });
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }
        
        return result;
    }
}
```

### BulkInsertStrategy Class
```csharp
public class BulkInsertStrategy : IBulkInsertStrategy
{
    private readonly IDbConnection _connection;
    
    public BulkInsertStrategy(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    
    public async Task<BulkResult<T>> ExecuteBatchAsync<T>(IEnumerable<T> entities, EntityMetadata metadata, BulkOptions options) where T : class
    {
        var result = new BulkResult<T>();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var entityList = entities.ToList();
            if (!entityList.Any())
            {
                return result;
            }
            
            var sql = GenerateInsertSql(metadata);
            var parameters = CreateParameters(entityList, metadata);
            
            using var transaction = options.UseTransaction ? _connection.BeginTransaction() : null;
            try
            {
                var affectedRows = await _connection.ExecuteAsync(sql, parameters, transaction, options.CommandTimeout);
                result.AffectedRows = affectedRows;
                result.ProcessedEntities.AddRange(entityList);
                
                if (transaction != null)
                {
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }
                throw;
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add(new BulkError
            {
                Index = -1,
                Entity = null,
                Message = ex.Message,
                Exception = ex
            });
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }
        
        return result;
    }
    
    private string GenerateInsertSql(EntityMetadata metadata)
    {
        var columns = metadata.Properties
            .Where(p => !p.IsIdentity)
            .Select(p => p.ColumnName)
            .ToArray();
        
        var parameters = columns.Select(c => $"@{c}").ToArray();
        
        return $"INSERT INTO {metadata.TableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameters)})";
    }
    
    private List<object> CreateParameters<T>(IEnumerable<T> entities, EntityMetadata metadata) where T : class
    {
        var parameters = new List<object>();
        
        foreach (var entity in entities)
        {
            var parameter = new Dictionary<string, object>();
            
            foreach (var property in metadata.Properties.Where(p => !p.IsIdentity))
            {
                var value = property.GetValue(entity);
                parameter[property.ColumnName] = value ?? DBNull.Value;
            }
            
            parameters.Add(parameter);
        }
        
        return parameters;
    }
}
```

### BatchProcessor Class
```csharp
public class BatchProcessor : IBatchProcessor
{
    public IEnumerable<IEnumerable<T>> CreateBatches<T>(IEnumerable<T> items, int batchSize)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (batchSize <= 0) throw new ArgumentException("Batch size must be positive", nameof(batchSize));
        
        var itemList = items.ToList();
        if (!itemList.Any())
        {
            yield break;
        }
        
        for (int i = 0; i < itemList.Count; i += batchSize)
        {
            var batch = itemList.Skip(i).Take(batchSize);
            yield return batch;
        }
    }
    
    public async Task<BulkResult<T>> ProcessBatchesAsync<T>(
        IEnumerable<IEnumerable<T>> batches,
        Func<IEnumerable<T>, Task<BulkResult<T>>> batchProcessor,
        BulkOptions options) where T : class
    {
        var result = new BulkResult<T>();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            foreach (var batch in batches)
            {
                var batchResult = await batchProcessor(batch);
                result.AffectedRows += batchResult.AffectedRows;
                result.ProcessedEntities.AddRange(batchResult.ProcessedEntities);
                result.Errors.AddRange(batchResult.Errors);
                
                if (options.StopOnError && batchResult.Errors.Any())
                {
                    break;
                }
            }
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }
        
        return result;
    }
}
```

### Usage Examples
```csharp
// Basic bulk operations
public class UserService
{
    private readonly IBulkOperations _bulkOperations;
    
    public UserService(IBulkOperations bulkOperations)
    {
        _bulkOperations = bulkOperations;
    }
    
    public async Task<int> CreateUsersAsync(IEnumerable<User> users)
    {
        return await _bulkOperations.BulkInsertAsync(users);
    }
    
    public async Task<int> UpdateUsersAsync(IEnumerable<User> users)
    {
        return await _bulkOperations.BulkUpdateAsync(users);
    }
    
    public async Task<int> DeleteUsersAsync(IEnumerable<User> users)
    {
        return await _bulkOperations.BulkDeleteAsync(users);
    }
    
    public async Task<int> UpsertUsersAsync(IEnumerable<User> users)
    {
        return await _bulkOperations.BulkUpsertAsync(users);
    }
}

// Advanced bulk operations with options
public class AdvancedUserService
{
    private readonly IBulkOperations _bulkOperations;
    
    public AdvancedUserService(IBulkOperations bulkOperations)
    {
        _bulkOperations = bulkOperations;
    }
    
    public async Task<BulkResult<User>> CreateUsersWithResultAsync(IEnumerable<User> users)
    {
        var options = new BulkOptions
        {
            BatchSize = 500,
            CommandTimeout = 60,
            UseTransaction = true,
            StopOnError = false,
            ReturnAffectedRows = true,
            ValidateEntities = true
        };
        
        return await _bulkOperations.BulkInsertWithResultAsync(users, options);
    }
    
    public async Task<BulkResult<User>> UpdateUsersWithResultAsync(IEnumerable<User> users)
    {
        var options = new BulkOptions
        {
            BatchSize = 1000,
            CommandTimeout = 30,
            UseTransaction = true,
            StopOnError = true,
            ReturnAffectedRows = true,
            ValidateEntities = true
        };
        
        return await _bulkOperations.BulkUpdateWithResultAsync(users, options);
    }
    
    public async Task<BulkResult<User>> DeleteUsersWithResultAsync(IEnumerable<User> users)
    {
        var options = new BulkOptions
        {
            BatchSize = 2000,
            CommandTimeout = 45,
            UseTransaction = true,
            StopOnError = false,
            ReturnAffectedRows = true,
            ValidateEntities = false
        };
        
        return await _bulkOperations.BulkDeleteWithResultAsync(users, options);
    }
}

// Error handling
public class BulkOperationService
{
    private readonly IBulkOperations _bulkOperations;
    
    public BulkOperationService(IBulkOperations bulkOperations)
    {
        _bulkOperations = bulkOperations;
    }
    
    public async Task<bool> ProcessUsersWithErrorHandlingAsync(IEnumerable<User> users)
    {
        try
        {
            var result = await _bulkOperations.BulkInsertWithResultAsync(users);
            
            if (!result.Success)
            {
                Console.WriteLine($"Bulk operation completed with {result.ErrorCount} errors:");
                
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"Error at index {error.Index}: {error.Message}");
                    if (error.Exception != null)
                    {
                        Console.WriteLine($"Exception: {error.Exception.Message}");
                    }
                }
                
                return false;
            }
            
            Console.WriteLine($"Bulk operation completed successfully. Affected rows: {result.AffectedRows}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Bulk operation failed: {ex.Message}");
            return false;
        }
    }
}
```

## 🧪 Test Cases

### Bulk Operations Tests
- [ ] Bulk insert operations
- [ ] Bulk update operations
- [ ] Bulk delete operations
- [ ] Bulk upsert operations
- [ ] Error handling
- [ ] Performance testing

### Batch Processing Tests
- [ ] Batch creation
- [ ] Batch processing
- [ ] Batch size configuration
- [ ] Memory optimization

### Strategy Tests
- [ ] Insert strategy
- [ ] Update strategy
- [ ] Delete strategy
- [ ] Upsert strategy

### Integration Tests
- [ ] End-to-end bulk operations
- [ ] Transaction support
- [ ] Error recovery
- [ ] Performance testing

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Basic bulk operations
- [ ] Advanced bulk operations
- [ ] Error handling
- [ ] Performance optimization
- [ ] Best practices

### Bulk Operations Guide
- [ ] Bulk operation types
- [ ] Configuration options
- [ ] Performance considerations
- [ ] Common scenarios
- [ ] Troubleshooting

## 🔍 Code Review Checklist

- [ ] Code follows .NET naming conventions
- [ ] All public members have XML documentation
- [ ] Error handling is appropriate
- [ ] Unit tests cover all scenarios
- [ ] Code is readable and maintainable
- [ ] Performance is optimized
- [ ] Memory usage is efficient
- [ ] Thread safety considerations

## 🚀 Next Steps

After completing this task:
1. Move to Phase 3.4: Lazy Loading
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on bulk operations design
- [ ] Performance considerations for bulk operations
- [ ] Integration with existing features
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
