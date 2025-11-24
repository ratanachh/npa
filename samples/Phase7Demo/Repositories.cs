using NPA.Core.Annotations;
using NPA.Core.Repositories;

namespace Phase7Demo;

[Repository]
public interface ICustomerRepository : IRepository<Customer, int>
{
    // Basic CRUD methods inherited from IRepository
    // No relationship methods generated (Orders has MappedBy - inverse side)
}

[Repository]
public interface IOrderRepository : IRepository<Order, int>
{
    // Basic CRUD methods inherited from IRepository
    // PLUS generated relationship methods:
    // - Task<Order?> GetByIdWithCustomerAsync(int id) [Customer is ManyToOne, Eager, Owner]
}

[Repository]
public interface IOrderItemRepository : IRepository<OrderItem, int>
{
    // Basic CRUD methods inherited from IRepository
    // PLUS generated relationship methods:
    // - Task<OrderItem?> GetByIdWithOrderAsync(int id) [Order is ManyToOne, Eager, Owner]
}

[Repository]
public interface IUserRepository : IRepository<User, int>
{
    // Basic CRUD methods inherited from IRepository
    // OneToOne relationships with User and UserProfile for bidirectional demo
}


[Repository]
public interface IProductRepository : IRepository<Product, Guid>
{
    
}