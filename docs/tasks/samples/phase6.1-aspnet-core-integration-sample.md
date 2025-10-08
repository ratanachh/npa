# Phase 6.1: ASP.NET Core Integration Sample

> **⚠️ PLANNED FEATURE**: This sample describes functionality that will be available once core features are complete. This document serves as a design reference for future ASP.NET Core integration patterns.

## 📋 Task Overview

**Objective**: Create a complete ASP.NET Core Web API demonstrating NPA integration with Dapper and best practices.

**Priority**: High  
**Estimated Time**: 8-10 hours  
**Dependencies**: Phase 1-3 features (currently Phase 1.1-1.3 available)  
**Target Framework**: .NET 8.0  
**Sample Name**: AspNetCoreIntegrationSample  
**Status**: 📋 Planned for Phase 6

## 🎯 Success Criteria

- [ ] RESTful Web API with CRUD endpoints
- [ ] Dependency injection integration
- [ ] Repository pattern implementation
- [ ] Transaction management
- [ ] Error handling middleware
- [ ] API documentation (Swagger)
- [ ] Unit and integration tests
- [ ] Docker containerization
- [ ] Production-ready code

## 📝 Application Architecture

```
AspNetCoreIntegrationSample/
├── Controllers/
│   ├── CustomersController.cs
│   ├── OrdersController.cs
│   └── ProductsController.cs
├── Models/
│   ├── Entities/
│   ├── DTOs/
│   └── Requests/
├── Repositories/
│   ├── ICustomerRepository.cs
│   ├── CustomerRepository.cs
│   └── UnitOfWork.cs
├── Services/
│   ├── ICustomerService.cs
│   └── CustomerService.cs
├── Middleware/
│   ├── ErrorHandlingMiddleware.cs
│   └── TransactionMiddleware.cs
├── Configuration/
│   └── NpaConfiguration.cs
└── Program.cs
```

## 💻 Key Features

### 1. Dependency Injection
```csharp
// Program.cs
builder.Services.AddNpa(options =>
{
    options.UsePostgreSql(connectionString);
    options.UseRepositoryPattern();
    options.EnableCaching();
    options.EnablePerformanceMonitoring();
});

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

### 2. RESTful Controller
```csharp
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    
    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var customers = await _customerService.GetAllAsync(page, pageSize);
        return Ok(customers);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetById(long id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null)
            return NotFound();
        
        return Ok(customer);
    }
    
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerRequest request)
    {
        var customer = await _customerService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult<CustomerDto>> Update(long id, [FromBody] UpdateCustomerRequest request)
    {
        var customer = await _customerService.UpdateAsync(id, request);
        if (customer == null)
            return NotFound();
        
        return Ok(customer);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var success = await _customerService.DeleteAsync(id);
        if (!success)
            return NotFound();
        
        return NoContent();
    }
}
```

### 3. Repository Implementation
```csharp
public class CustomerRepository : ICustomerRepository
{
    private readonly IEntityManager _entityManager;
    
    public CustomerRepository(IEntityManager entityManager)
    {
        _entityManager = entityManager;
    }
    
    public async Task<Customer?> FindByIdAsync(long id)
    {
        return await _entityManager.FindAsync<Customer>(id);
    }
    
    public async Task<IEnumerable<Customer>> FindAllAsync(int page, int pageSize)
    {
        return await _entityManager.CreateQuery<Customer>()
            .OrderBy("Id")
            .SetFirstResult((page - 1) * pageSize)
            .SetMaxResults(pageSize)
            .GetResultListAsync();
    }
    
    public async Task<Customer?> FindByEmailAsync(string email)
    {
        return await _entityManager.CreateQuery<Customer>()
            .Where("Email = @email")
            .SetParameter("email", email)
            .GetSingleResultAsync();
    }
    
    public async Task SaveAsync(Customer customer)
    {
        await _entityManager.PersistAsync(customer);
    }
    
    public async Task UpdateAsync(Customer customer)
    {
        await _entityManager.MergeAsync(customer);
    }
    
    public async Task DeleteAsync(long id)
    {
        await _entityManager.RemoveAsync<Customer>(id);
    }
}
```

### 4. Unit of Work Pattern
```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly IEntityManager _entityManager;
    private IDbTransaction? _transaction;
    
    public ICustomerRepository Customers { get; }
    public IOrderRepository Orders { get; }
    public IProductRepository Products { get; }
    
    public UnitOfWork(
        IEntityManager entityManager,
        ICustomerRepository customerRepository,
        IOrderRepository orderRepository,
        IProductRepository productRepository)
    {
        _entityManager = entityManager;
        Customers = customerRepository;
        Orders = orderRepository;
        Products = productRepository;
    }
    
    public async Task BeginTransactionAsync()
    {
        _transaction = await _entityManager.BeginTransactionAsync();
    }
    
    public async Task CommitAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
    
    public async Task RollbackAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
    
    public async Task<int> SaveChangesAsync()
    {
        await _entityManager.FlushAsync();
        return 1; // Return number of affected entities
    }
}
```

### 5. Error Handling Middleware
```csharp
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    
    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "Entity not found");
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message, errors = ex.Errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "An error occurred" });
        }
    }
}
```

## 🧪 Testing Strategy

### Unit Tests
```csharp
public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock;
    private readonly CustomerService _service;
    
    public CustomerServiceTests()
    {
        _repositoryMock = new Mock<ICustomerRepository>();
        _service = new CustomerService(_repositoryMock.Object);
    }
    
    [Fact]
    public async Task GetByIdAsync_ExistingCustomer_ReturnsCustomer()
    {
        // Arrange
        var customer = new Customer { Id = 1, FirstName = "John", LastName = "Doe" };
        _repositoryMock.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(customer);
        
        // Act
        var result = await _service.GetByIdAsync(1);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.FirstName);
    }
}
```

### Integration Tests
```csharp
public class CustomersControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public CustomersControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task GetAll_ReturnsSuccessStatusCode()
    {
        var response = await _client.GetAsync("/api/customers");
        response.EnsureSuccessStatusCode();
    }
}
```

## 📦 Docker Support

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AspNetCoreIntegrationSample.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AspNetCoreIntegrationSample.dll"]
```

## 📚 Learning Outcomes

- ASP.NET Core integration patterns
- RESTful API design
- Dependency injection
- Repository and Unit of Work patterns
- Error handling strategies
- Testing strategies
- Production deployment

---

*Created: October 8, 2025*  
*Status: ⏳ Pending*
