# Phase 3.1: Transaction Management

## 📋 Task Overview

**Objective**: Implement comprehensive transaction management that supports both declarative and programmatic transaction handling with full Dapper integration.

**Priority**: High  
**Estimated Time**: 3-4 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.6 (All previous phases)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] ITransaction interface is complete
- [ ] Transaction class implements all transaction operations
- [ ] Declarative transaction support works
- [ ] Programmatic transaction support works
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## 📝 Detailed Requirements

### 1. ITransaction Interface
- **Purpose**: Defines the contract for transaction operations
- **Methods**:
  - `Task CommitAsync()` - Commit the transaction
  - `Task RollbackAsync()` - Rollback the transaction
  - `Task DisposeAsync()` - Dispose the transaction
  - `bool IsActive` - Check if transaction is active
  - `IsolationLevel IsolationLevel` - Get isolation level
  - `IDbTransaction DbTransaction` - Get underlying transaction

### 2. Transaction Class
- **Purpose**: Implementation of transaction operations
- **Dependencies**: IDbConnection, IDbTransaction
- **Features**:
  - Transaction lifecycle management
  - Isolation level support
  - Error handling
  - Performance optimization
  - Resource cleanup

### 3. Declarative Transaction Support
- **TransactionalAttribute**: Mark methods for automatic transaction
- **Transaction Scope**: Automatic transaction management
- **Error Handling**: Automatic rollback on exceptions
- **Performance**: Optimize transaction usage

### 4. Programmatic Transaction Support
- **BeginTransactionAsync()**: Start new transaction
- **ExecuteInTransactionAsync()**: Execute code in transaction
- **Nested Transactions**: Support for nested transactions
- **Transaction Propagation**: Handle transaction propagation

### 5. Integration with EntityManager
- **Automatic Transaction**: Use existing transaction if available
- **Transaction Context**: Pass transaction context
- **Batch Operations**: Optimize batch operations with transactions
- **Error Handling**: Handle transaction errors

## 🏗️ Implementation Plan

### Step 1: Create Interfaces
1. Create `ITransaction` interface
2. Create `ITransactionManager` interface
3. Create `ITransactionScope` interface
4. Create `ITransactionContext` interface

### Step 2: Implement Core Classes
1. Create `Transaction` class
2. Create `TransactionManager` class
3. Create `TransactionScope` class
4. Create `TransactionContext` class

### Step 3: Implement Transaction Operations
1. Implement `BeginTransactionAsync()` method
2. Implement `CommitAsync()` method
3. Implement `RollbackAsync()` method
4. Implement `DisposeAsync()` method

### Step 4: Add Declarative Support
1. Create `TransactionalAttribute` class
2. Implement transaction interception
3. Add automatic transaction management
4. Add error handling

### Step 5: Add Programmatic Support
1. Implement `ExecuteInTransactionAsync()` method
2. Add nested transaction support
3. Add transaction propagation
4. Add performance optimizations

### Step 6: Integrate with EntityManager
1. Update EntityManager for transaction support
2. Add transaction context passing
3. Optimize batch operations
4. Add error handling

### Step 7: Create Unit Tests
1. Test transaction operations
2. Test declarative transactions
3. Test programmatic transactions
4. Test error scenarios

### Step 8: Add Documentation
1. XML documentation comments
2. Usage examples
3. Transaction guide
4. Best practices

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

### ITransaction Interface
```csharp
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
    bool IsActive { get; }
    IsolationLevel IsolationLevel { get; }
    IDbTransaction DbTransaction { get; }
}
```

### Transaction Class
```csharp
public class Transaction : ITransaction
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _dbTransaction;
    private bool _disposed = false;
    
    public Transaction(IDbConnection connection, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _dbTransaction = connection.BeginTransaction(isolationLevel);
        IsolationLevel = isolationLevel;
    }
    
    public async Task CommitAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Transaction));
        
        if (!IsActive)
            throw new InvalidOperationException("Transaction is not active");
        
        try
        {
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

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
