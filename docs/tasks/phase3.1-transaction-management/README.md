# Phase 3.1: Transaction Management

## 📋 Task Overview

**Objective**: Implement comprehensive transaction management that supports both declarative and programmatic transaction handling with full Dapper integration. This phase will also enhance the Flush mechanism to support **deferred execution** and **operation batching**.

**Priority**: High  
**Estimated Time**: 4-5 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.6 (All previous phases)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] ITransaction interface is complete (async & sync)
- [ ] Transaction class implements all transaction operations
- [ ] Enhanced Flush mechanism with deferred execution
- [ ] Operation batching and optimization
- [ ] Declarative transaction support works
- [ ] Programmatic transaction support works
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## [IN PROGRESS] Flush Strategy Enhancement

### Background: Evolution from Phase 1.2

**Phase 1.2 Implementation:**
- Operations execute SQL **immediately** (no deferral)
- `Flush()` has limited utility (only for non-generated IDs)
- Simple but no batching capabilities

**Phase 3.1 Enhancement:**
- Operations are **queued** in change tracker
- SQL execution **deferred** until Flush or Commit
- Enables **batching** for performance
- True **unit-of-work** pattern

## 📝 Detailed Requirements

### 1. Enhanced Flush Mechanism

**Current State (Phase 1.2):**
```csharp
// Immediate execution
await entityManager.PersistAsync(user);  // Executes INSERT immediately
await entityManager.FlushAsync();        // Only processes queued operations
```

**Enhanced State (Phase 3.1):**
```csharp
// Deferred execution with batching
using var tx = await entityManager.BeginTransactionAsync();

await entityManager.PersistAsync(user1);  // Queued
await entityManager.PersistAsync(user2);  // Queued
await entityManager.MergeAsync(user3);    // Queued

await entityManager.FlushAsync();         // Batches and executes all operations
await tx.CommitAsync();                   // Commits transaction
```

**Key Changes:**
- [ ] Refactor `Persist()` to queue operations instead of immediate execution
- [ ] Refactor `Merge()` to queue operations instead of immediate execution
- [ ] Refactor `Remove()` to queue operations instead of immediate execution
- [ ] Enhance `Flush()` to batch and execute all queued operations
- [ ] Add transaction-aware execution
- [ ] Optimize for reduced database round-trips

**Benefits:**
- [Completed] **Performance**: Batch multiple operations
- [Completed] **Transaction Safety**: All-or-nothing execution
- [Completed] **Consistency**: Deferred constraint checking
- [Completed] **Unit of Work**: True JPA pattern
- [Completed] **Scalability**: Reduced database round-trips

### 2. ITransaction Interface
- **Purpose**: Defines the contract for transaction operations
- **Methods** (Async & Sync):
  - `Task CommitAsync()` / `void Commit()` - Commit the transaction
  - `Task RollbackAsync()` / `void Rollback()` - Rollback the transaction
  - `Task DisposeAsync()` / `void Dispose()` - Dispose the transaction
  - `bool IsActive` - Check if transaction is active
  - `IsolationLevel IsolationLevel` - Get isolation level
  - `IDbTransaction DbTransaction` - Get underlying transaction

### 3. Transaction Class
- **Purpose**: Implementation of transaction operations
- **Dependencies**: IDbConnection, IDbTransaction
- **Features**:
  - Transaction lifecycle management
  - Isolation level support
  - Error handling
  - Performance optimization
  - Resource cleanup
  - Integration with enhanced Flush mechanism

**Key Integration:**
- When a transaction is active, all operations are queued
- `Flush()` executes operations within the transaction context
- `Commit()` automatically flushes before committing
- `Rollback()` clears the change tracker

### 4. Declarative Transaction Support
- **TransactionalAttribute**: Mark methods for automatic transaction
- **Transaction Scope**: Automatic transaction management
- **Error Handling**: Automatic rollback on exceptions
- **Performance**: Optimize transaction usage
- **Flush Integration**: Automatic flush before commit

### 5. Programmatic Transaction Support
- **BeginTransactionAsync() / BeginTransaction()**: Start new transaction
- **ExecuteInTransactionAsync() / ExecuteInTransaction()**: Execute code in transaction
- **Nested Transactions**: Support for nested transactions
- **Transaction Propagation**: Handle transaction propagation
- **Flush Control**: Manual flush control within transactions

### 6. Integration with EntityManager

**Enhanced EntityManager Methods:**
- [ ] Add `BeginTransactionAsync()` / `BeginTransaction()` to IEntityManager
- [ ] Add `GetCurrentTransaction()` to IEntityManager
- [ ] Add `HasActiveTransaction()` property
- [ ] Update `Persist()`, `Merge()`, `Remove()` to check for active transaction
- [ ] Update `Flush()` to use transaction context if available

**Transaction-Aware Behavior:**
```csharp
// Without transaction: Immediate execution (backward compatible)
entityManager.Persist(user);  // Executes immediately

// With transaction: Deferred execution (new in 3.1)
using var tx = await entityManager.BeginTransactionAsync();
entityManager.Persist(user1);  // Queued
entityManager.Persist(user2);  // Queued
entityManager.Flush();         // Batches and executes
tx.Commit();                   // Commits
```

**Key Features:**
- **Automatic Transaction Detection**: Operations detect active transaction
- **Transaction Context**: Pass transaction to all operations
- **Batch Operations**: Optimize batch operations with transactions
- **Error Handling**: Handle transaction errors gracefully
- **Flush on Commit**: Automatically flush before commit

## 🏗️ Implementation Plan

### Step 1: Enhance Change Tracking for Deferred Execution
**Goal:** Refactor change tracker to support queuing operations

1. [ ] Update `IChangeTracker` interface:
   - Add `QueueOperation(entity, state, sqlGenerator)` method
   - Add `GetQueuedOperations()` method
   - Add `ClearQueue()` method
2. [ ] Update `ChangeTracker` class:
   - Implement operation queue
   - Track SQL generation delegates
   - Support transaction-aware behavior
3. [ ] Add operation priority (INSERT before UPDATE, DELETE last)

### Step 2: Refactor EntityManager for Deferred Execution
**Goal:** Make operations queue by default when transaction is active

1. [ ] Add `HasActiveTransaction()` property to EntityManager
2. [ ] Add `GetCurrentTransaction()` method
3. [ ] Refactor `Persist()` to check for active transaction:
   - If transaction active: Queue operation
   - If no transaction: Execute immediately (backward compatible)
4. [ ] Refactor `Merge()` similarly
5. [ ] Refactor `Remove()` similarly
6. [ ] Enhance `Flush()` to batch execute queued operations

### Step 3: Create Transaction Interfaces
1. [ ] Create `ITransaction` interface (async & sync)
2. [ ] Create `ITransactionManager` interface
3. [ ] Add methods to `IEntityManager`:
   - `BeginTransactionAsync()` / `BeginTransaction()`
   - `GetCurrentTransaction()`

### Step 4: Implement Transaction Core Classes
1. [ ] Create `Transaction` class (async & sync)
2. [ ] Implement transaction lifecycle management
3. [ ] Add isolation level support
4. [ ] Add error handling
5. [ ] Implement auto-flush before commit

### Step 5: Implement Transaction Operations
1. [ ] Implement `BeginTransactionAsync()` / `BeginTransaction()`
2. [ ] Implement `CommitAsync()` / `Commit()`
   - Auto-flush before commit
   - Error handling
3. [ ] Implement `RollbackAsync()` / `Rollback()`
   - Clear change tracker queue
4. [ ] Implement `DisposeAsync()` / `Dispose()`

### Step 6: Add Declarative Support
1. [ ] Create `TransactionalAttribute` class
2. [ ] Implement transaction interception
3. [ ] Add automatic transaction management
4. [ ] Add automatic flush and commit

### Step 7: Add Programmatic Support
1. [ ] Implement `ExecuteInTransactionAsync()` / `ExecuteInTransaction()`
2. [ ] Add nested transaction support (savepoints)
3. [ ] Add transaction propagation
4. [ ] Add performance optimizations

### Step 8: Integrate Enhanced Flush
1. [ ] Batch INSERT operations
2. [ ] Batch UPDATE operations  
3. [ ] Order operations (INSERT → UPDATE → DELETE)
4. [ ] Optimize SQL generation
5. [ ] Add error handling for batch failures

### Step 9: Create Unit Tests
1. [ ] Test transaction operations
2. [ ] Test declarative transactions
3. [ ] Test programmatic transactions
4. [ ] Test enhanced flush batching
5. [ ] Test deferred execution
6. [ ] Test error scenarios
7. [ ] Test backward compatibility

### Step 10: Add Documentation
1. [ ] XML documentation comments
2. [ ] Usage examples for transactions
3. [ ] Enhanced flush strategy documentation
4. [ ] Migration guide from Phase 1.2
5. [ ] Performance comparison
6. [ ] Best practices

## 📁 File Structure

```
src/NPA.Core/Core/
├── ITransaction.cs
├── Transaction.cs
├── ITransactionManager.cs
├── TransactionManager.cs
├── ITransactionScope.cs
├── TransactionScope.cs
├── ITransactionContext.cs
├── TransactionContext.cs
└── TransactionalAttribute.cs

tests/NPA.Core.Tests/Transactions/
├── TransactionTests.cs
├── TransactionManagerTests.cs
├── TransactionScopeTests.cs
├── TransactionalAttributeTests.cs
└── TransactionIntegrationTests.cs
```

## 💻 Code Examples

### Enhanced Flush Strategy

**Before Phase 3.1 (Immediate Execution):**
```csharp
// Each operation executes immediately
await entityManager.PersistAsync(user1);    // INSERT executed
await entityManager.PersistAsync(user2);    // INSERT executed
await entityManager.MergeAsync(user3);      // UPDATE executed
// 3 database round-trips
```

**After Phase 3.1 (Deferred Execution with Batching):**
```csharp
// With transaction: Operations are queued
using var tx = await entityManager.BeginTransactionAsync();

await entityManager.PersistAsync(user1);    // Queued
await entityManager.PersistAsync(user2);    // Queued
await entityManager.MergeAsync(user3);      // Queued

await entityManager.FlushAsync();           // Batches: 2 INSERTs + 1 UPDATE
await tx.CommitAsync();                     // 1 transaction commit
// 1 batch operation + 1 commit = Better performance
```

**Without transaction: Backward compatible immediate execution:**
```csharp
// No transaction: Executes immediately (Phase 1.2 behavior)
await entityManager.PersistAsync(user);  // Still executes immediately
```

### ITransaction Interface
```csharp
public interface ITransaction : IAsyncDisposable, IDisposable
{
    // Async methods
    Task CommitAsync();
    Task RollbackAsync();
    ValueTask DisposeAsync();
    
    // Sync methods
    void Commit();
    void Rollback();
    void Dispose();
    
    // Properties
    bool IsActive { get; }
    IsolationLevel IsolationLevel { get; }
    IDbTransaction DbTransaction { get; }
}
```

### Transaction Class (with Enhanced Flush Integration)
```csharp
public class Transaction : ITransaction
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _dbTransaction;
    private readonly IEntityManager _entityManager;
    private bool _disposed = false;
    
    public Transaction(IDbConnection connection, IEntityManager entityManager, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        _dbTransaction = connection.BeginTransaction(isolationLevel);
        IsolationLevel = isolationLevel;
    }
    
    // Async commit with auto-flush
    public async Task CommitAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Transaction));
        
        if (!IsActive)
            throw new InvalidOperationException("Transaction is not active");
        
        try
        {
            // IMPORTANT: Auto-flush before commit to execute queued operations
            await _entityManager.FlushAsync();
            
            _dbTransaction.Commit();
        }
        catch (Exception ex)
        {
            throw new TransactionException("Failed to commit transaction", ex);
        }
    }
    
    // Sync commit with auto-flush
    public void Commit()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Transaction));
        
        if (!IsActive)
            throw new InvalidOperationException("Transaction is not active");
        
        try
        {
            // IMPORTANT: Auto-flush before commit
            _entityManager.Flush();
            
            _dbTransaction.Commit();
        }
        catch (Exception ex)
        {
            throw new TransactionException("Failed to commit transaction", ex);
        }
    }
    
    public async Task RollbackAsync()
    {
        if (_disposed)
            return;
        
        try
        {
            // Clear queued operations on rollback
            await _entityManager.ClearAsync();
            _dbTransaction.Rollback();
        }
        catch (Exception ex)
        {
            throw new TransactionException("Failed to rollback transaction", ex);
        }
    }
    
    public void Rollback()
    {
        if (_disposed)
            return;
        
        try
        {
            _entityManager.Clear();
            _dbTransaction.Rollback();
        }
        catch (Exception ex)
        {
            throw new TransactionException("Failed to rollback transaction", ex);
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            try
            {
                if (IsActive)
                {
                    await RollbackAsync();
                }
                _dbTransaction.Dispose();
            }
            finally
            {
                _disposed = true;
            }
        }
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                if (IsActive)
                {
                    Rollback();
                }
                _dbTransaction.Dispose();
            }
            finally
            {
                _disposed = true;
            }
        }
    }
    
    public bool IsActive => !_disposed && _dbTransaction != null;
    public IsolationLevel IsolationLevel { get; }
    public IDbTransaction DbTransaction => _dbTransaction;
}
```

### TransactionalAttribute
```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class TransactionalAttribute : Attribute
{
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
    public bool ReadOnly { get; set; } = false;
    public int Timeout { get; set; } = 30; // seconds
    
    public TransactionalAttribute() { }
    
    public TransactionalAttribute(IsolationLevel isolationLevel)
    {
        IsolationLevel = isolationLevel;
    }
}
```

### TransactionManager Class
```csharp
public class TransactionManager : ITransactionManager
{
    private readonly IDbConnection _connection;
    private readonly AsyncLocal<ITransaction> _currentTransaction = new();
    
    public TransactionManager(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    
    public async Task<ITransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        var transaction = new Transaction(_connection, isolationLevel);
        _currentTransaction.Value = transaction;
        return transaction;
    }
    
    public ITransaction? GetCurrentTransaction()
    {
        return _currentTransaction.Value;
    }
    
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        using var transaction = await BeginTransactionAsync(isolationLevel);
        try
        {
            var result = await operation();
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    public async Task ExecuteInTransactionAsync(Func<Task> operation, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        using var transaction = await BeginTransactionAsync(isolationLevel);
        try
        {
            await operation();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

### Usage Examples
```csharp
// Declarative transaction
[Transactional]
public async Task<Order> CreateOrderWithItemsAsync(long userId, List<OrderItemDto> items)
{
    var order = new Order { UserId = userId, OrderDate = DateTime.UtcNow };
    await entityManager.PersistAsync(order);
    
    foreach (var itemDto in items)
    {
        var orderItem = new OrderItem { OrderId = order.Id, ProductId = itemDto.ProductId };
        await entityManager.PersistAsync(orderItem);
    }
    
    return order; // Transaction commits automatically
}

// Programmatic transaction
public async Task TransferFundsAsync(long fromAccountId, long toAccountId, decimal amount)
{
    using var transaction = await entityManager.BeginTransactionAsync();
    try
    {
        var fromAccount = await entityManager.FindAsync<Account>(fromAccountId);
        var toAccount = await entityManager.FindAsync<Account>(toAccountId);
        
        fromAccount.Balance -= amount;
        toAccount.Balance += amount;
        
        await entityManager.MergeAsync(fromAccount);
        await entityManager.MergeAsync(toAccount);
        
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}

// Functional transaction
public async Task<Order> CreateOrderAsync(Order order)
{
    return await transactionManager.ExecuteInTransactionAsync(async () =>
    {
        await entityManager.PersistAsync(order);
        await entityManager.FlushAsync();
        return order;
    });
}
```

## 🧪 Test Cases

### Transaction Operations Tests
- [ ] Begin transaction successfully
- [ ] Commit transaction successfully
- [ ] Rollback transaction successfully
- [ ] Dispose transaction correctly
- [ ] Handle transaction errors

### Declarative Transaction Tests
- [ ] Automatic transaction creation
- [ ] Automatic commit on success
- [ ] Automatic rollback on exception
- [ ] Isolation level handling
- [ ] Timeout handling

### Programmatic Transaction Tests
- [ ] Manual transaction management
- [ ] Nested transaction support
- [ ] Transaction propagation
- [ ] Error handling
- [ ] Resource cleanup

### Integration Tests
- [ ] EntityManager integration
- [ ] Batch operation optimization
- [ ] Performance testing
- [ ] Concurrent transaction handling
- [ ] Deadlock prevention

### Error Handling Tests
- [ ] Connection errors
- [ ] Transaction errors
- [ ] Timeout errors
- [ ] Deadlock errors
- [ ] Resource cleanup errors

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Basic transaction operations
- [ ] Declarative transactions
- [ ] Programmatic transactions
- [ ] Performance considerations
- [ ] Best practices
- [ ] Error handling

### Transaction Guide
- [ ] Transaction types
- [ ] Isolation levels
- [ ] Transaction patterns
- [ ] Performance optimization
- [ ] Common pitfalls

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
1. Move to Phase 3.2: Cascade Operations
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on transaction management
- [ ] Performance considerations for transactions
- [ ] Integration with Dapper transactions
- [ ] Error message localization
- [ ] Backward compatibility verification
- [ ] Performance benchmarking (immediate vs deferred)

## 💡 Detailed Flush Strategy Explanation

### Why the Change?

**Problem with Current Approach (Phase 1.2):**
```csharp
// Each call = 1 database round-trip
entityManager.Persist(user1);     // Round-trip 1: INSERT
entityManager.Persist(user2);     // Round-trip 2: INSERT
entityManager.Merge(user3);       // Round-trip 3: UPDATE
entityManager.Remove(user4);      // Round-trip 4: DELETE
// Total: 4 round-trips for 4 operations
```

**Solution with Enhanced Flush (Phase 3.1):**
```csharp
using var tx = await entityManager.BeginTransactionAsync();

// All operations queued (0 round-trips yet)
entityManager.Persist(user1);
entityManager.Persist(user2);
entityManager.Merge(user3);
entityManager.Remove(user4);

// Flush: Batch execute (1 round-trip for all)
entityManager.Flush();  // Batches: 2 INSERTs, 1 UPDATE, 1 DELETE

// Commit
tx.Commit();  // Round-trip 2: Transaction commit
// Total: 2 round-trips for 4 operations = 50% reduction
```

### Implementation Strategy

**1. Transaction Detection:**
```csharp
public void Persist<T>(T entity) where T : class
{
    if (HasActiveTransaction())
    {
        // Queue operation for batch execution
        _changeTracker.QueueOperation(entity, EntityState.Added, () => GenerateInsertSql(entity));
    }
    else
    {
        // Execute immediately (backward compatible)
        ExecuteInsert(entity);
    }
}
```

**2. Operation Queueing:**
```csharp
public class ChangeTracker
{
    private readonly Queue<QueuedOperation> _operationQueue = new();
    
    public void QueueOperation(object entity, EntityState state, Func<string> sqlGenerator)
    {
        _operationQueue.Enqueue(new QueuedOperation
        {
            Entity = entity,
            State = state,
            SqlGenerator = sqlGenerator,
            Priority = GetPriority(state)
        });
    }
    
    private int GetPriority(EntityState state)
    {
        return state switch
        {
            EntityState.Added => 1,      // INSERT first
            EntityState.Modified => 2,   // UPDATE second
            EntityState.Deleted => 3,    // DELETE last
            _ => 0
        };
    }
}
```

**3. Batch Execution:**
```csharp
public void Flush()
{
    var operations = _changeTracker
        .GetQueuedOperations()
        .OrderBy(op => op.Priority)  // Order: INSERT → UPDATE → DELETE
        .GroupBy(op => op.State);    // Group by operation type
    
    foreach (var group in operations)
    {
        if (group.Key == EntityState.Added)
        {
            // Batch all INSERTs
            var inserts = group.Select(op => op.SqlGenerator()).ToList();
            ExecuteBatchInserts(inserts);
        }
        else if (group.Key == EntityState.Modified)
        {
            // Batch all UPDATEs
            var updates = group.Select(op => op.SqlGenerator()).ToList();
            ExecuteBatchUpdates(updates);
        }
        else if (group.Key == EntityState.Deleted)
        {
            // Batch all DELETEs
            var deletes = group.Select(op => op.SqlGenerator()).ToList();
            ExecuteBatchDeletes(deletes);
        }
    }
    
    _changeTracker.ClearQueue();
}
```

### Backward Compatibility

**Phase 1.2 code continues to work:**
```csharp
// Without transaction: Immediate execution (unchanged)
entityManager.Persist(user);      // Executes immediately
entityManager.Merge(user);        // Executes immediately

// With explicit flush (still works)
entityManager.Persist(user);
entityManager.Flush();            // Works but not needed without transaction
```

**Phase 3.1 enables new patterns:**
```csharp
// With transaction: Deferred execution (new capability)
using var tx = await entityManager.BeginTransactionAsync();
entityManager.Persist(user1);     // Queued
entityManager.Persist(user2);     // Queued  
entityManager.Flush();            // Batch execute
tx.Commit();                      // Commit
```

### Performance Impact

**Scenario: 100 entity operations**

**Phase 1.2 (Immediate):**
- 100 database round-trips
- No batching
- Simple but inefficient

**Phase 3.1 (Deferred with Transaction):**
- ~5-10 round-trips (depending on operation grouping)
- Full batching support
- 90-95% reduction in round-trips
- Significantly better performance

### Migration Checklist

**For existing code:**
- [ ] Review all `Persist()` calls
- [ ] Review all `Merge()` calls
- [ ] Review all `Remove()` calls
- [ ] Add transactions where batching would help
- [ ] Keep existing code for simple operations
- [ ] Test backward compatibility

**For new code:**
- [ ] Use transactions for multi-entity operations
- [ ] Leverage batching for performance
- [ ] Use deferred execution patterns
- [ ] Follow new best practices

---

*Created: [Current Date]*  
*Last Updated: 2025-01-10*  
*Status: Planned - Enhanced with Flush Strategy Documentation*
