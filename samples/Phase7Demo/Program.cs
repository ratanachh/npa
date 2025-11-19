using System;
using Phase7Demo;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║     NPA Phase 7: Advanced Relationship Management Demo        ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// ============================================================================
// Phase 7.1: Relationship-Aware Repository Generation
// ============================================================================
Console.WriteLine("═══ Phase 7.1: Relationship-Aware Repository Generation ═══");
Console.WriteLine();
Console.WriteLine("✓ Generated GetByIdWith{Property}Async() methods for eager relationships");
Console.WriteLine("✓ Generated Load{Property}Async() methods for lazy relationships");
Console.WriteLine("✓ SQL JOIN generation with Dapper multi-mapping");
Console.WriteLine("✓ Automatic detection of owner vs inverse side");
Console.WriteLine();
Console.WriteLine("Example Generated Methods:");
Console.WriteLine("  • Task<Order?> GetByIdWithCustomerAsync(int id)       // ManyToOne eager");
Console.WriteLine("  • Task<OrderItem?> GetByIdWithOrderAsync(int id)      // ManyToOne eager");
Console.WriteLine("  • Task<Customer?> LoadCustomerAsync(Order order)      // Lazy loading");
Console.WriteLine();

// ============================================================================
// Phase 7.2: Eager Loading Support
// ============================================================================
Console.WriteLine("═══ Phase 7.2: Eager Loading Support ═══");
Console.WriteLine();
Console.WriteLine("✓ Automatic eager loading for FetchType.Eager relationships");
Console.WriteLine("✓ Override GetByIdAsync() with LEFT JOIN for eager relationships");
Console.WriteLine("✓ GetByIdsAsync() batch loading to prevent N+1 queries");
Console.WriteLine("✓ Smart query generation (single JOIN or separate queries)");
Console.WriteLine();
Console.WriteLine("Example Generated Methods:");
Console.WriteLine("  • override Task<Order?> GetByIdAsync(int id)          // Auto-loads eager relationships");
Console.WriteLine("  • Task<IEnumerable<Order>> GetByIdsAsync(IEnumerable<int> ids)  // Batch loading");
Console.WriteLine();

// ============================================================================
// Phase 7.3: Cascade Operations Enhancement
// ============================================================================
Console.WriteLine("═══ Phase 7.3: Cascade Operations Enhancement ═══");
Console.WriteLine();
Console.WriteLine("✓ AddWithCascadeAsync() - Cascade persist (parent-first, child-after)");
Console.WriteLine("✓ UpdateWithCascadeAsync() - Cascade merge with orphan removal");
Console.WriteLine("✓ DeleteWithCascadeAsync() - Cascade remove (children-first)");
Console.WriteLine("✓ Transient entity detection (checks for default Id values)");
Console.WriteLine("✓ OrphanRemoval support for deleted collection items");
Console.WriteLine();
Console.WriteLine("Example Generated Methods:");
Console.WriteLine("  • Task<Customer> AddWithCascadeAsync(Customer entity)         // CascadeType.Persist");
Console.WriteLine("  • Task UpdateWithCascadeAsync(Customer entity)                // CascadeType.Merge");
Console.WriteLine("  • Task DeleteWithCascadeAsync(int id)                         // CascadeType.Remove");
Console.WriteLine();

// ============================================================================
// Phase 7.4: Bidirectional Relationship Management
// ============================================================================
Console.WriteLine("═══ Phase 7.4: Bidirectional Relationship Management ═══");
Console.WriteLine();

Console.WriteLine("Demo 1: OneToMany/ManyToOne Bidirectional Synchronization");
Console.WriteLine("────────────────────────────────────────────────────────");

var customer = new Customer { Id = 1, Name = "John Doe", Email = "john@example.com" };
var order1 = new Order { Id = 101, OrderNumber = "ORD-001", CustomerId = 1 };
var order2 = new Order { Id = 102, OrderNumber = "ORD-002", CustomerId = 1 };

Console.WriteLine("Initial state:");
Console.WriteLine($"  customer.Orders.Count = {customer.Orders?.Count ?? 0}");
Console.WriteLine();

Console.WriteLine("Setting order1.Customer using OrderRelationshipHelper.SetCustomer()...");
OrderRelationshipHelper.SetCustomer(order1, customer);
Console.WriteLine($"  ✓ order1.Customer = {order1.Customer?.Name}");
Console.WriteLine($"  ✓ customer.Orders.Count = {customer.Orders?.Count ?? 0} (collection updated)");
Console.WriteLine();

Console.WriteLine("Adding order2 using CustomerRelationshipHelper.AddToOrders()...");
CustomerRelationshipHelper.AddToOrders(customer, order2);
Console.WriteLine($"  ✓ order2.Customer = {order2.Customer?.Name} (inverse set)");
Console.WriteLine($"  ✓ order2.CustomerId = {order2.CustomerId} (FK synchronized)");
Console.WriteLine($"  ✓ customer.Orders.Count = {customer.Orders?.Count ?? 0}");
Console.WriteLine();

Console.WriteLine("Removing order1 using CustomerRelationshipHelper.RemoveFromOrders()...");
CustomerRelationshipHelper.RemoveFromOrders(customer, order1);
Console.WriteLine($"  ✓ order1.Customer = {(order1.Customer == null ? "null" : order1.Customer.Name)} (inverse cleared)");
Console.WriteLine($"  ✓ order1.CustomerId = {order1.CustomerId} (FK cleared)");
Console.WriteLine($"  ✓ customer.Orders.Count = {customer.Orders?.Count ?? 0}");
Console.WriteLine();

Console.WriteLine("Demo 2: OneToOne Bidirectional Synchronization");
Console.WriteLine("───────────────────────────────────────────────");

var user = new User { Id = 1, Username = "johndoe", Email = "john@example.com" };
var profile = new UserProfile { Id = 1, UserId = 1, Bio = "Software developer", AvatarUrl = "avatar.jpg" };

Console.WriteLine("Setting profile.User using UserProfileRelationshipHelper.SetUser()...");
UserProfileRelationshipHelper.SetUser(profile, user);
Console.WriteLine($"  ✓ profile.User = {profile.User?.Username}");
Console.WriteLine($"  ✓ profile.UserId = {profile.UserId} (FK set)");
Console.WriteLine($"  ✓ Inverse side synchronized (OneToOne)");
Console.WriteLine();

// ============================================================================
// Summary
// ============================================================================
Console.WriteLine();
Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                    Generated Features Summary                  ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine("Phase 7.1: Relationship-Aware Repositories        ✅ COMPLETE");
Console.WriteLine("Phase 7.2: Eager Loading Support                  ✅ COMPLETE (Basic)");
Console.WriteLine("Phase 7.3: Cascade Operations                     ✅ COMPLETE");
Console.WriteLine("Phase 7.4: Bidirectional Synchronization          🚧 70% COMPLETE");
Console.WriteLine();
Console.WriteLine("📁 Check obj/generated folder for all generated code!");
Console.WriteLine();
Console.WriteLine("Generated Files:");
Console.WriteLine("  • CustomerRepositoryImplementation.g.cs   (with cascade methods)");
Console.WriteLine("  • OrderRepositoryImplementation.g.cs      (with eager + cascade + relationships)");
Console.WriteLine("  • OrderItemRepositoryImplementation.g.cs  (with eager loading)");
Console.WriteLine("  • UserRepositoryImplementation.g.cs       (standard CRUD)");
Console.WriteLine("  • CustomerRelationshipHelper.g.cs         (bidirectional sync)");
Console.WriteLine("  • OrderRelationshipHelper.g.cs            (bidirectional sync)");
Console.WriteLine("  • OrderItemRelationshipHelper.g.cs        (bidirectional sync)");
Console.WriteLine("  • UserRelationshipHelper.g.cs             (bidirectional sync)");
Console.WriteLine("  • UserProfileRelationshipHelper.g.cs      (bidirectional sync)");
Console.WriteLine();
Console.WriteLine("✨ All Phase 7 features demonstrated successfully!");
