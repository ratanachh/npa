# Phase 9: Comprehensive JPA & NPA Repository Reference

This document serves as a comprehensive guide for designing robust JPA-style entities and utilizing NPA repositories, covering basic to expert features. It compares JPA capabilities with NPA's current implementation status.

## Overview

Phase 9 consolidates all advanced JPA features, multi-table mapping concepts, and comprehensive entity/repository patterns. This phase documents what NPA supports, what's planned, and provides workarounds for features not yet implemented.

## Table of Contents

1. [Entity Definition and Core Mapping](#1-entity-definition-and-core-mapping)
2. [Advanced Entity Mapping](#2-advanced-entity-mapping)
   - [Value Objects (`@Embedded` / `@Embeddable`)](#21-value-objects-embedded--embeddable)
   - [Composite Primary Keys](#22-composite-primary-keys-embeddedid)
   - [Secondary Table Mapping](#23-secondary-table-mapping-secondarytable)
   - [Element Collections](#24-element-collections-elementcollection)
3. [Repository Pattern Features](#3-repository-pattern-features)
   - [Repository Programming Model](#31-repository-programming-model)
   - [Query Derivation Mechanism](#32-query-derivation-mechanism)
   - [Performance Optimization: Entity Graphs](#36-performance-optimization-entity-graphs-entitygraph)
   - [Auditing](#311-auditing-createddate-lastmodifieddate)
   - [Dynamic Filtering with Specifications](#310-dynamic-filtering-with-specifications-jpaspecificationexecutor)
4. [Transactional Management](#4-transactional-management)
5. [Important Distinctions: NPA vs JPA](#5-important-distinctions-npa-vs-jpa)
6. [Feature Comparison Matrix](#6-feature-comparison-matrix)
7. [Best Practices](#7-best-practices)
8. [Migration Guide: JPA to NPA](#8-migration-guide-jpa-to-npa)
9. [Future Enhancements](#9-future-enhancements)

---

## 1. 🧬 Entity Definition and Core Mapping

### 1.1 Basic Entity Mapping

**JPA Example:**
```java
@Entity
@Table(name = "customer_orders")
public class Order implements Serializable {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(name = "order_date", nullable = false)
    private LocalDateTime orderDate = LocalDateTime.now();
}
```

**NPA Equivalent (✅ Supported):**
```csharp
[Entity]
[Table("customer_orders")]
public class Order
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }
    
    [Column("order_date", IsNullable = false)]
    public DateTime OrderDate { get; set; } = DateTime.Now;
}
```

### 1.2 Primary Key Generation

**JPA Strategies:**
- `GenerationType.IDENTITY` - Database auto-increment
- `GenerationType.SEQUENCE` - Database sequence
- `GenerationType.TABLE` - Table-based generator
- `GenerationType.AUTO` - Let provider choose

**NPA Support:**
- ✅ `GenerationType.Identity` - Supported for SQL Server, MySQL, PostgreSQL, SQLite
- ✅ `GenerationType.Sequence` - Supported for PostgreSQL
- ❌ `GenerationType.Table` - Not implemented
- ❌ `GenerationType.Auto` - Not implemented (use explicit strategy)

### 1.3 Column Mapping

**JPA:**
```java
@Column(name = "order_number", nullable = false, length = 50, unique = true)
private String orderNumber;
```

**NPA (✅ Supported):**
```csharp
[Column("order_number", IsNullable = false, Length = 50, IsUnique = true)]
public string OrderNumber { get; set; }
```

### 1.4 Custom Type Conversion

**JPA:**
```java
@Convert(converter = StatusConverter.class)
private OrderStatus status;
```

**NPA Status:** ❌ Not implemented. Use string properties or separate conversion logic.

**NPA Workaround:**
```csharp
[Column("status")]
public string StatusString { get; set; }

// Property for type-safe access
public OrderStatus Status
{
    get => Enum.Parse<OrderStatus>(StatusString);
    set => StatusString = value.ToString();
}
```

### 1.5 Entity Relationships

#### Many-to-One (✅ Supported)

**JPA:**
```java
@ManyToOne(fetch = FetchType.LAZY)
@JoinColumn(name = "customer_id", nullable = false)
private Customer customer;
```

**NPA:**
```csharp
[ManyToOne(Fetch = FetchType.Lazy)]
[JoinColumn("customer_id")]
public Customer Customer { get; set; } = null!;

// Optional: Expose FK directly for convenience
// [Column("customer_id")]
// public long CustomerId { get; set; }
```

#### One-to-Many (✅ Supported)

**JPA:**
```java
@OneToMany(mappedBy = "order", cascade = CascadeType.ALL, orphanRemoval = true, fetch = FetchType.LAZY)
private Set<OrderLine> orderLines = new HashSet<>();
```

**NPA:**
```csharp
[OneToMany(MappedBy = nameof(OrderLine.Order), Cascade = CascadeType.All, OrphanRemoval = true, Fetch = FetchType.Lazy)]
public ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
```

#### One-to-One (✅ Supported)

**JPA:**
```java
@OneToOne
@JoinColumn(name = "profile_id")
private UserProfile profile;
```

**NPA:**
```csharp
[OneToOne]
[JoinColumn("profile_id")]
public UserProfile? Profile { get; set; }
```

#### Many-to-Many (✅ Supported)

**JPA:**
```java
@ManyToMany
@JoinTable(name = "user_roles",
    joinColumns = @JoinColumn(name = "user_id"),
    inverseJoinColumns = @JoinColumn(name = "role_id"))
private Set<Role> roles = new HashSet<>();
```

**NPA:**
```csharp
[ManyToMany]
[JoinTable("user_roles",
    JoinColumns = new[] { "user_id" },
    InverseJoinColumns = new[] { "role_id" })]
public ICollection<Role> Roles { get; set; } = new List<Role>();
```

---

## 2. 🗃️ Advanced Entity Mapping

### 2.1 Value Objects (`@Embedded` / `@Embeddable`)

**JPA Example:**
```java
// Address.java
@Embeddable
public class Address {
    @Column(name = "street_address")
    private String street;
    
    @Column(length = 50)
    private String city;
    
    private String zipCode;
}

// Customer.java
@Entity
public class Customer {
    @Id
    private Long id;
    
    @Embedded
    private Address shippingAddress;
    
    @Embedded
    @AttributeOverrides({
        @AttributeOverride(name = "street", column = @Column(name = "billing_street")),
        @AttributeOverride(name = "city", column = @Column(name = "billing_city"))
    })
    private Address billingAddress;
}
```

**NPA Status:** ❌ Not implemented

**NPA Workaround:**
```csharp
// Option 1: Flat properties
[Entity]
[Table("customers")]
public class Customer
{
    [Id]
    public long Id { get; set; }
    
    [Column("shipping_street")]
    public string ShippingStreet { get; set; }
    
    [Column("shipping_city")]
    public string ShippingCity { get; set; }
    
    [Column("shipping_zip_code")]
    public string ShippingZipCode { get; set; }
}

// Option 2: Composition (not mapped)
public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string ZipCode { get; set; }
}

[Entity]
[Table("customers")]
public class Customer
{
    [Id]
    public long Id { get; set; }
    
    // Not mapped - for code organization only
    public Address ShippingAddress { get; set; }
    
    // Mapped properties
    [Column("shipping_street")]
    public string ShippingStreet { get; set; }
    
    [Column("shipping_city")]
    public string ShippingCity { get; set; }
    
    [Column("shipping_zip_code")]
    public string ShippingZipCode { get; set; }
}
```

### 2.2 Composite Primary Keys (`@EmbeddedId`)

**JPA Example:**
```java
// OrderLineId.java
@Embeddable
public class OrderLineId implements Serializable {
    private Long orderId;
    private Integer lineNumber;
    
    // Must implement equals() and hashCode()
    @Override
    public boolean equals(Object o) { ... }
    
    @Override
    public int hashCode() { ... }
}

// OrderLine.java
@Entity
public class OrderLine {
    @EmbeddedId
    private OrderLineId id;
    
    @MapsId("orderId")
    @ManyToOne
    @JoinColumn(name = "order_id")
    private Order order;
    
    @Column(name = "item_price")
    private BigDecimal price;
}
```

**NPA Status:** ✅ Supported (with different syntax)

**NPA Implementation:**
```csharp
[Entity]
[Table("order_lines")]
public class OrderLine
{
    // Composite key properties
    [Id]
    [Column("order_id")]
    public long OrderId { get; set; }
    
    [Id]
    [Column("line_number")]
    public int LineNumber { get; set; }
    
    [ManyToOne]
    [JoinColumn("order_id")]
    public Order Order { get; set; } = null!;
    
    [Column("item_price")]
    public decimal Price { get; set; }
}

// Usage
var orderLine = await entityManager.FindAsync<OrderLine>(
    new CompositeKey { { "OrderId", 1L }, { "LineNumber", 1 } });
```

**Key Differences:**
- NPA uses multiple `[Id]` attributes instead of `@EmbeddedId`
- NPA uses `CompositeKey` dictionary for lookups
- No need for separate key class (though you can create one for type safety)

### 2.3 Secondary Table Mapping (`@SecondaryTable`)

**JPA Example:**
```java
@Entity
@Table(name = "meal")
@SecondaryTable(name = "allergens", 
    pkJoinColumns = @PrimaryKeyJoinColumn(name = "meal_id"))
public class Meal {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(name = "name")
    private String name;
    
    // Columns from secondary table
    @Column(name = "peanuts", table = "allergens")
    private boolean peanuts;
    
    @Column(name = "celery", table = "allergens")
    private boolean celery;
}
```

**NPA Status:** ❌ Not implemented

**NPA Workaround:**
```csharp
[Entity]
[Table("meal")]
public class Meal
{
    [Id]
    public long Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
    
    [OneToOne]
    [JoinColumn("meal_id")]
    public Allergens Allergens { get; set; }
}

[Entity]
[Table("allergens")]
public class Allergens
{
    [Id]
    [Column("meal_id")]
    public long MealId { get; set; }
    
    [OneToOne]
    [PrimaryKeyJoinColumn("meal_id")]
    public Meal Meal { get; set; }
    
    [Column("peanuts")]
    public bool Peanuts { get; set; }
    
    [Column("celery")]
    public bool Celery { get; set; }
}
```

### 2.4 Element Collections (`@ElementCollection`)

**JPA Example:**
```java
@Entity
@Table(name = "users")
public class User {
    @Id
    private Long id;
    
    @ElementCollection
    @CollectionTable(name = "user_phones", 
        joinColumns = @JoinColumn(name = "user_id"))
    @Column(name = "phone_number")
    private List<String> phoneNumbers;
    
    @ElementCollection
    @CollectionTable(name = "user_addresses",
        joinColumns = @JoinColumn(name = "user_id"))
    private List<Address> addresses;
}
```

**NPA Status:** ❌ Not implemented

**NPA Workaround:**
```csharp
[Entity]
[Table("users")]
public class User
{
    [Id]
    public long Id { get; set; }
    
    [OneToMany(MappedBy = nameof(UserPhone.User))]
    public ICollection<UserPhone> PhoneNumbers { get; set; } = new List<UserPhone>();
}

[Entity]
[Table("user_phones")]
public class UserPhone
{
    [Id]
    public long Id { get; set; }
    
    [ManyToOne]
    [JoinColumn("user_id")]
    public User User { get; set; }
    
    [Column("phone_number")]
    public string PhoneNumber { get; set; }
}
```

### 2.5 Inheritance Mapping (`@Inheritance` - JOINED Strategy)

**JPA Example:**
```java
// Base Class: Product (Stores common fields)
@Entity
@Inheritance(strategy = InheritanceType.JOINED)
public abstract class Product {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    private String name;
    private BigDecimal price;
    
    // ... Getters and Setters
}

// Subclass: DigitalProduct (Maps to its own table, links back to Product PK)
@Entity
@Table(name = "digital_products")
@PrimaryKeyJoinColumn(name = "product_id") // Foreign Key to the parent table
public class DigitalProduct extends Product {
    private String downloadUrl;
    // ...
}

// Subclass: PhysicalProduct
@Entity
@Table(name = "physical_products")
@PrimaryKeyJoinColumn(name = "product_id")
public class PhysicalProduct extends Product {
    private Double weight;
    private String dimensions;
    // ...
}
```

**Database Schema:**
```sql
CREATE TABLE products (
    id BIGINT PRIMARY KEY IDENTITY,
    name NVARCHAR(100) NOT NULL,
    price DECIMAL(10,2) NOT NULL
);

CREATE TABLE digital_products (
    product_id BIGINT PRIMARY KEY,
    download_url NVARCHAR(255),
    FOREIGN KEY (product_id) REFERENCES products(id)
);

CREATE TABLE physical_products (
    product_id BIGINT PRIMARY KEY,
    weight DECIMAL(10,2),
    dimensions NVARCHAR(50),
    FOREIGN KEY (product_id) REFERENCES products(id)
);
```

**NPA Status:** ❌ Not implemented

**NPA Workaround:**
```csharp
// Use separate entities with OneToOne relationships
[Entity]
[Table("products")]
public class Product
{
    [Id]
    public long Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
    
    [Column("price")]
    public decimal Price { get; set; }
    
    // Discriminator column to identify type
    [Column("product_type")]
    public string ProductType { get; set; }
}

[Entity]
[Table("digital_products")]
public class DigitalProduct
{
    [Id]
    [Column("product_id")]
    public long ProductId { get; set; }
    
    [OneToOne]
    [PrimaryKeyJoinColumn("product_id")]
    public Product Product { get; set; }
    
    [Column("download_url")]
    public string DownloadUrl { get; set; }
}

[Entity]
[Table("physical_products")]
public class PhysicalProduct
{
    [Id]
    [Column("product_id")]
    public long ProductId { get; set; }
    
    [OneToOne]
    [PrimaryKeyJoinColumn("product_id")]
    public Product Product { get; set; }
    
    [Column("weight")]
    public double Weight { get; set; }
    
    [Column("dimensions")]
    public string Dimensions { get; set; }
}
```

### 2.6 Custom Data Type Conversion (`AttributeConverter`)

**JPA Example:**
```java
// 1. The Enum/Custom Type
public enum OrderStatus {
    CREATED("C"), SHIPPED("S"), DELIVERED("D");
    
    private final String code;
    
    OrderStatus(String code) { 
        this.code = code; 
    }
    
    public String getCode() { 
        return code; 
    }
    
    public static OrderStatus fromCode(String code) {
        for (OrderStatus status : values()) {
            if (status.code.equals(code)) return status;
        }
        throw new IllegalArgumentException("Unknown status code: " + code);
    }
}

// 2. The Converter (implements AttributeConverter)
@Converter(autoApply = true) // Apply this converter automatically to all OrderStatus fields
public class OrderStatusConverter implements AttributeConverter<OrderStatus, String> {
    @Override
    public String convertToDatabaseColumn(OrderStatus status) {
        return status == null ? null : status.getCode();
    }
    
    @Override
    public OrderStatus convertToEntityAttribute(String code) {
        return code == null ? null : OrderStatus.fromCode(code);
    }
}

// 3. The Entity Usage
@Entity
public class Order {
    @Id
    private Long id;
    
    // No @Convert needed if autoApply=true on the converter
    private OrderStatus status;
    
    // ...
}
```

**NPA Status:** ❌ Not implemented

**NPA Workaround:**
```csharp
// Option 1: Store as string, use property for type-safe access
[Entity]
[Table("orders")]
public class Order
{
    [Id]
    public long Id { get; set; }
    
    [Column("status")]
    public string StatusString { get; set; }
    
    // Property for type-safe access
    public OrderStatus Status
    {
        get => OrderStatus.FromCode(StatusString);
        set => StatusString = value.GetCode();
    }
}

public enum OrderStatus
{
    Created,
    Shipped,
    Delivered
}

public static class OrderStatusExtensions
{
    private static readonly Dictionary<OrderStatus, string> CodeMap = new()
    {
        { OrderStatus.Created, "C" },
        { OrderStatus.Shipped, "S" },
        { OrderStatus.Delivered, "D" }
    };
    
    public static string GetCode(this OrderStatus status) => CodeMap[status];
    
    public static OrderStatus FromCode(string code)
    {
        return CodeMap.FirstOrDefault(kvp => kvp.Value == code).Key
            ?? throw new ArgumentException($"Unknown status code: {code}");
    }
}

// Option 2: Use a custom repository method for conversion
[Repository]
public interface IOrderRepository : IRepository<Order, long>
{
    [Query("SELECT * FROM orders WHERE status = @statusCode")]
    Task<IEnumerable<Order>> FindByStatusAsync(string statusCode);
}

// Usage
var orders = await orderRepository.FindByStatusAsync(OrderStatus.Shipped.GetCode());
```

---

## 3. 💾 Repository Pattern Features

### 3.1 Repository Programming Model

Spring Data JPA provides a hierarchy of repository interfaces that abstract away boilerplate code:

**JPA (Spring Data JPA):**
```java
// Base marker interface
public interface Repository<T, ID> { }

// Provides basic CRUD operations
public interface CrudRepository<T, ID> extends Repository<T, ID> {
    <S extends T> S save(S entity);
    Optional<T> findById(ID id);
    Iterable<T> findAll();
    void delete(T entity);
    // ... more CRUD methods
}

// Adds pagination and sorting
public interface PagingAndSortingRepository<T, ID> extends CrudRepository<T, ID> {
    Iterable<T> findAll(Sort sort);
    Page<T> findAll(Pageable pageable);
}

// Most commonly used - combines all features + JPA-specific methods
public interface JpaRepository<T, ID> extends PagingAndSortingRepository<T, ID> {
    void flush();
    <S extends T> S saveAndFlush(S entity);
    <S extends T> List<S> saveAllAndFlush(Iterable<S> entities);
    // ... more JPA methods
}
```

**NPA (✅ Supported):**
```csharp
// Base repository interface
public interface IRepository<T, TKey> where T : class
{
    Task<T?> GetByIdAsync(TKey id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    // ... more CRUD methods
}

// Read-only repository for query-only scenarios
public interface IReadOnlyRepository<T, TKey> where T : class
{
    Task<T?> GetByIdAsync(TKey id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    // ... query methods only
}

// Base implementation
public abstract class BaseRepository<T, TKey> : IRepository<T, TKey> where T : class
{
    // Default implementation provided
    // Can be extended for custom behavior
}
```

**Key Differences:**
- NPA uses `IRepository<T, TKey>` instead of `JpaRepository<T, ID>`
- NPA provides `IReadOnlyRepository` for query-only scenarios
- NPA uses async methods by default (`Task<T>`)
- NPA doesn't have separate `CrudRepository` and `PagingAndSortingRepository` - features are combined
- NPA uses source generators to create implementations automatically

### 3.2 Query Derivation Mechanism

**JPA (Spring Data JPA):**

Spring parses method names and automatically generates JPQL queries:

```java
public interface UserRepository extends JpaRepository<User, Long> {
    // Derived query: findByLastNameAndFirstName
    // Generated: SELECT u FROM User u WHERE u.lastName = ?1 AND u.firstName = ?2
    List<User> findByLastNameAndFirstName(String lastName, String firstName);
    
    // Supports various keywords
    List<User> findByAgeBetween(int minAge, int maxAge);
    List<User> findBySalaryLessThan(BigDecimal salary);
    List<User> findByEmailContaining(String email);
    List<User> findByCreatedDateAfter(LocalDateTime date);
    List<User> findByLastNameStartingWith(String prefix);
    List<User> findByLastNameEndingWith(String suffix);
    List<User> findByMiddleNameIsNull();
    List<User> findByIsActiveTrue();
    List<User> findByLastNameOrderByFirstNameAsc(String lastName);
    List<User> findDistinctByEmail(String email);
}
```

**Supported Keywords:**
- `And`, `Or` - Logical operators
- `Between`, `LessThan`, `LessThanEqual`, `GreaterThan`, `GreaterThanEqual` - Comparisons
- `Like`, `NotLike`, `StartingWith`, `EndingWith`, `Containing` - String operations
- `IsNull`, `IsNotNull`, `IsTrue`, `IsFalse` - Null/boolean checks
- `OrderBy`, `Asc`, `Desc` - Sorting
- `Distinct` - Unique results
- `First`, `Top` - Limit results

**NPA Status:** ❌ Not implemented (method name parsing)

**NPA Alternative (✅ Supported):**
```csharp
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    // Use NamedQuery or Query attributes
    [NamedQuery("User.FindByLastNameAndFirstName", 
        "SELECT u FROM User u WHERE u.LastName = :lastName AND u.FirstName = :firstName")]
    Task<IEnumerable<User>> FindByLastNameAndFirstNameAsync(string lastName, string firstName);
    
    // Or use LINQ expressions
    Task<IEnumerable<User>> FindAsync(
        Expression<Func<User, bool>> predicate,
        Expression<Func<User, object>>? orderBy = null,
        bool descending = false);
}

// Usage with LINQ
var users = await repository.FindAsync(
    u => u.LastName == lastName && u.FirstName == firstName);

var usersByAge = await repository.FindAsync(
    u => u.Age >= minAge && u.Age <= maxAge,
    orderBy: u => u.LastName,
    descending: false);
```

**Key Differences:**
- NPA doesn't parse method names - use `[NamedQuery]` or `[Query]` attributes
- NPA uses LINQ expressions for dynamic queries
- NPA requires explicit query definition or LINQ predicates

### 3.3 Basic Repository Interface

**JPA (Spring Data JPA):**
```java
@Repository
public interface OrderRepository extends JpaRepository<Order, Long> {
    // Standard CRUD methods inherited:
    // save(), findById(), findAll(), delete(), etc.
}
```

**NPA (✅ Supported):**
```csharp
[Repository]
public interface IOrderRepository : IRepository<Order, long>
{
    // Standard CRUD methods inherited:
    // AddAsync(), GetByIdAsync(), GetAllAsync(), DeleteAsync(), etc.
}
```

### 3.2 Query Method Derivation

**JPA (Spring Data JPA):**
```java
// Automatic query generation from method name
Set<Order> findByOrderDateAfterOrderByIdAsc(LocalDateTime date);

List<Order> findByCustomerIdAndStatus(Long customerId, OrderStatus status);

Optional<Order> findFirstByOrderByOrderDateDesc();
```

**NPA Status:** ❌ Not implemented (method name parsing)

**NPA Alternative (✅ Supported):**
```csharp
[Repository]
public interface IOrderRepository : IRepository<Order, long>
{
    // Use NamedQuery or Query attributes
    [NamedQuery("Order.FindByDateAfter", 
        "SELECT o FROM Order o WHERE o.OrderDate > :date ORDER BY o.Id ASC")]
    Task<IEnumerable<Order>> FindByOrderDateAfterAsync(DateTime date);
    
    [Query("SELECT * FROM customer_orders WHERE customer_id = @customerId AND status = @status")]
    Task<IEnumerable<Order>> FindByCustomerIdAndStatusAsync(long customerId, string status);
    
    [Query("SELECT TOP 1 * FROM customer_orders ORDER BY order_date DESC")]
    Task<Order?> FindFirstByOrderDateDescAsync();
}
```

### 3.4 JPQL Queries (`@Query`)

**JPA:**
```java
@Query("SELECT o FROM Order o JOIN FETCH o.orderLines WHERE o.id = :orderId")
Order findByIdWithLines(@Param("orderId") Long id);

@Query("SELECT o FROM Order o WHERE o.customer.id = :customerId")
List<Order> findByCustomerId(@Param("customerId") Long customerId);
```

**NPA (✅ Supported with CPQL):**
```csharp
[Repository]
public interface IOrderRepository : IRepository<Order, long>
{
    // CPQL (similar to JPQL)
    [Query("SELECT o FROM Order o JOIN o.OrderLines WHERE o.Id = :orderId")]
    Task<Order?> GetByIdWithLinesAsync(long orderId);
    
    [Query("SELECT o FROM Order o WHERE o.Customer.Id = :customerId")]
    Task<IEnumerable<Order>> FindByCustomerIdAsync(long customerId);
}
```

**Key Differences:**
- NPA uses CPQL (C# Persistence Query Language) instead of JPQL
- Syntax is similar but uses C# property names (PascalCase)
- JOIN FETCH is supported for eager loading

### 3.5 Native SQL Queries

**JPA:**
```java
@Query(value = "SELECT o.id, o.order_date FROM customer_orders o WHERE o.status = :status", 
       nativeQuery = true)
Set<Object[]> findNativeOrdersByStatus(@Param("status") String status);
```

**NPA (✅ Supported):**
```csharp
[Repository]
public interface IOrderRepository : IRepository<Order, long>
{
    [Query("SELECT id, order_date FROM customer_orders WHERE status = @status", NativeQuery = true)]
    Task<IEnumerable<dynamic>> FindNativeOrdersByStatusAsync(string status);
}
```

### 3.6 Performance Optimization: Entity Graphs (`@EntityGraph`)

**JPA (Spring Data JPA):**

`@EntityGraph` is used to specify which associated entities should be fetched eagerly to prevent the N+1 select problem:

```java
@Entity
public class Order {
    @Id
    private Long id;
    
    @ManyToOne(fetch = FetchType.LAZY)
    private Customer customer;
    
    @OneToMany(fetch = FetchType.LAZY)
    private List<OrderLine> orderLines;
}

public interface OrderRepository extends JpaRepository<Order, Long> {
    // Use default entity graph (defined on entity)
    @EntityGraph
    Optional<Order> findById(Long id);
    
    // Use named entity graph
    @EntityGraph("Order.withCustomerAndLines")
    List<Order> findAll();
    
    // Define inline entity graph
    @EntityGraph(attributePaths = {"customer", "orderLines"})
    List<Order> findByStatus(OrderStatus status);
}

// Named entity graph defined on entity
@Entity
@NamedEntityGraph(
    name = "Order.withCustomerAndLines",
    attributeNodes = {
        @NamedAttributeNode("customer"),
        @NamedAttributeNode("orderLines")
    }
)
public class Order {
    // ...
}
```

**NPA Status:** ❌ Not implemented

**NPA Alternative (✅ Supported):**
```csharp
// Option 1: Use JOIN FETCH in CPQL queries
[Repository]
public interface IOrderRepository : IRepository<Order, long>
{
    [Query("SELECT o FROM Order o JOIN o.Customer JOIN o.OrderLines WHERE o.Id = :id")]
    Task<Order?> GetByIdWithRelationsAsync(long id);
    
    [Query("SELECT o FROM Order o JOIN o.Customer JOIN o.OrderLines WHERE o.Status = :status")]
    Task<IEnumerable<Order>> FindByStatusWithRelationsAsync(OrderStatus status);
}

// Option 2: Use eager loading in repository methods
[Repository]
public interface IOrderRepository : IRepository<Order, long>
{
    // Generated method with eager loading
    Task<Order?> GetByIdWithCustomerAsync(long id);
    Task<Order?> GetByIdWithOrderLinesAsync(long id);
    Task<Order?> GetByIdWithCustomerAndOrderLinesAsync(long id);
}

// Option 3: Configure fetch type on relationship
[Entity]
[Table("orders")]
public class Order
{
    [Id]
    public long Id { get; set; }
    
    // Eager fetch for this relationship
    [ManyToOne(Fetch = FetchType.Eager)]
    [JoinColumn("customer_id")]
    public Customer Customer { get; set; }
}
```

**Key Differences:**
- NPA doesn't have `@EntityGraph` - use `JOIN FETCH` in CPQL or configure `Fetch = FetchType.Eager`
- NPA repository generators can create methods with eager loading
- NPA requires explicit JOIN FETCH in queries for ad-hoc eager loading

### 3.7 Locking Mechanisms

**JPA:**
```java
@Lock(LockModeType.PESSIMISTIC_WRITE)
Order findByIdForUpdate(Long id);
```

**NPA Status:** ❌ Not implemented

**NPA Workaround:**
```csharp
// Use database-specific SQL
[Query("SELECT * FROM customer_orders WHERE id = @id FOR UPDATE", NativeQuery = true)]
Task<Order?> GetByIdForUpdateAsync(long id);

// Or use transaction isolation
using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
var order = await repository.GetByIdAsync(id);
// ... modify order ...
await transaction.CommitAsync();
```

### 3.8 Custom Repository Methods

**JPA:**
```java
@Repository
public interface OrderRepository extends JpaRepository<Order, Long> {
    // Custom implementation
    List<Order> findComplexOrders(Criteria criteria);
}

// Implementation
public class OrderRepositoryImpl implements OrderRepositoryCustom {
    @PersistenceContext
    private EntityManager entityManager;
    
    @Override
    public List<Order> findComplexOrders(Criteria criteria) {
        // Custom logic
    }
}
```

**NPA (✅ Supported):**
```csharp
[Repository]
public interface IOrderRepository : IRepository<Order, long>
{
    // Custom methods with implementation
    Task<IEnumerable<Order>> FindComplexOrdersAsync(Criteria criteria);
}

// Generated implementation (or custom)
public class OrderRepositoryImplementation : BaseRepository<Order, long>, IOrderRepository
{
    public async Task<IEnumerable<Order>> FindComplexOrdersAsync(Criteria criteria)
    {
        // Custom logic using _connection
        var sql = BuildComplexQuery(criteria);
        return await _connection.QueryAsync<Order>(sql, criteria);
    }
}
```

### 3.9 Paging and Sorting

**JPA:**
```java
Page<Order> findByCustomerId(Long customerId, Pageable pageable);

List<Order> findAll(Sort sort);
```

**NPA (✅ Supported):**
```csharp
[Repository]
public interface IOrderRepository : IRepository<Order, long>
{
    // Using LINQ-style methods
    Task<IEnumerable<Order>> FindAsync(
        Expression<Func<Order, bool>> predicate,
        Expression<Func<Order, object>> orderBy,
        bool descending = false,
        int skip = 0,
        int take = 0);
}

// Usage
var orders = await repository.FindAsync(
    o => o.CustomerId == customerId,
    o => o.OrderDate,
    descending: true,
    skip: 0,
    take: 10);
```

### 3.10 Dynamic Filtering with Specifications (`JpaSpecificationExecutor`)

**JPA Example:**

The **Specification** pattern allows for building complex, dynamic queries programmatically, which is essential for advanced filtering UIs where predicates change based on user input.

#### A. Repository Setup

```java
// OrderRepository.java
public interface OrderRepository extends JpaRepository<Order, Long>, 
                                         JpaSpecificationExecutor<Order> {
    // No new methods needed here, the findAll(Specification, Pageable) 
    // method is inherited from JpaSpecificationExecutor.
}
```

#### B. The Specification Factory (`OrderSpecifications`)

A class to create reusable, composable filtering predicates.

```java
// OrderSpecifications.java
public class OrderSpecifications {

    // Specification 1: Filter by a minimum price
    public static Specification<Order> hasMinimumTotal(BigDecimal minTotal) {
        return (root, query, criteriaBuilder) -> 
            criteriaBuilder.greaterThanOrEqualTo(root.get("totalAmount"), minTotal);
    }

    // Specification 2: Filter by a specific status
    public static Specification<Order> hasStatus(OrderStatus status) {
        return (root, query, criteriaBuilder) -> 
            criteriaBuilder.equal(root.get("status"), status);
    }
    
    // Specification 3: Filter by customer name
    public static Specification<Order> byCustomerName(String name) {
        // Assume Customer is related via 'customer' field
        return (root, query, criteriaBuilder) -> 
            criteriaBuilder.like(root.get("customer").get("firstName"), name + "%");
    }
}
```

#### C. Service Usage

Specifications can be combined using the `and()` or `or()` methods.

```java
// OrderService.java (in a method)
public Page<Order> findFilteredOrders(BigDecimal min, OrderStatus status, int page) {
    
    // Combine two specifications using 'and'
    Specification<Order> finalSpec = OrderSpecifications.hasMinimumTotal(min)
                                        .and(OrderSpecifications.hasStatus(status));
    
    // Create Pageable for pagination (e.g., page 0, size 20, sorted by date)
    Pageable pageable = PageRequest.of(page, 20, Sort.by("orderDate").descending());

    // Execute the query using the combined Specification and Pageable
    return orderRepository.findAll(finalSpec, pageable);
}
```

**NPA Status:** ❌ Not implemented (no Criteria API equivalent)

**NPA Alternative (✅ Supported):**

NPA supports dynamic filtering using LINQ expressions and query builders:

```csharp
// Option 1: Using LINQ Expressions (Recommended)
[Repository]
public interface IOrderRepository : IRepository<Order, long>
{
    // Use Expression<Func<T, bool>> for dynamic predicates
    Task<IEnumerable<Order>> FindAsync(
        Expression<Func<Order, bool>>? predicate = null,
        Expression<Func<Order, object>>? orderBy = null,
        bool descending = false,
        int skip = 0,
        int take = 0);
}

// Specification-like factory class
public static class OrderSpecifications
{
    public static Expression<Func<Order, bool>> HasMinimumTotal(decimal minTotal)
    {
        return o => o.TotalAmount >= minTotal;
    }
    
    public static Expression<Func<Order, bool>> HasStatus(OrderStatus status)
    {
        return o => o.Status == status;
    }
    
    public static Expression<Func<Order, bool>> ByCustomerName(string name)
    {
        return o => o.Customer != null && o.Customer.FirstName.StartsWith(name);
    }
    
    // Combine specifications
    public static Expression<Func<Order, bool>> And(
        Expression<Func<Order, bool>> left,
        Expression<Func<Order, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(Order), "o");
        var leftBody = ReplaceParameter(left.Body, left.Parameters[0], parameter);
        var rightBody = ReplaceParameter(right.Body, right.Parameters[0], parameter);
        var combined = Expression.AndAlso(leftBody, rightBody);
        return Expression.Lambda<Func<Order, bool>>(combined, parameter);
    }
    
    private static Expression ReplaceParameter(Expression expression, 
        ParameterExpression oldParam, ParameterExpression newParam)
    {
        return new ParameterReplacer(oldParam, newParam).Visit(expression);
    }
}

// Usage
public async Task<IEnumerable<Order>> FindFilteredOrdersAsync(
    decimal minTotal, OrderStatus status, int page, int pageSize)
{
    var spec1 = OrderSpecifications.HasMinimumTotal(minTotal);
    var spec2 = OrderSpecifications.HasStatus(status);
    var combined = OrderSpecifications.And(spec1, spec2);
    
    return await _orderRepository.FindAsync(
        predicate: combined,
        orderBy: o => o.OrderDate,
        descending: true,
        skip: page * pageSize,
        take: pageSize);
}

// Option 2: Using CPQL with dynamic query building
public class OrderQueryBuilder
{
    private readonly List<string> _conditions = new();
    private readonly Dictionary<string, object> _parameters = new();
    
    public OrderQueryBuilder WithMinimumTotal(decimal minTotal)
    {
        _conditions.Add("o.TotalAmount >= :minTotal");
        _parameters["minTotal"] = minTotal;
        return this;
    }
    
    public OrderQueryBuilder WithStatus(OrderStatus status)
    {
        _conditions.Add("o.Status = :status");
        _parameters["status"] = status;
        return this;
    }
    
    public OrderQueryBuilder WithCustomerName(string name)
    {
        _conditions.Add("o.Customer.FirstName LIKE :customerName");
        _parameters["customerName"] = $"{name}%";
        return this;
    }
    
    public string BuildQuery()
    {
        var whereClause = _conditions.Count > 0 
            ? "WHERE " + string.Join(" AND ", _conditions)
            : "";
        return $"SELECT o FROM Order o {whereClause} ORDER BY o.OrderDate DESC";
    }
    
    public Dictionary<string, object> GetParameters() => _parameters;
}

// Usage
var queryBuilder = new OrderQueryBuilder()
    .WithMinimumTotal(100m)
    .WithStatus(OrderStatus.Shipped);
    
var query = entityManager.CreateQuery<Order>(queryBuilder.BuildQuery());
foreach (var param in queryBuilder.GetParameters())
{
    query.SetParameter(param.Key, param.Value);
}
var orders = await query.GetResultListAsync();
```

**Key Differences:**
- NPA uses LINQ expressions instead of JPA Criteria API
- Specifications are combined using expression tree manipulation or simple AND/OR logic
- CPQL can be used for more complex dynamic queries
- No built-in `JpaSpecificationExecutor` equivalent, but similar functionality via LINQ

---

## 4. 📝 Transactional Management

### 4.1 Transaction Demarcation

**JPA (Spring):**
```java
@Transactional
public void createOrder(Order order) {
    orderRepository.save(order);
    // Changes are automatically flushed on commit
}

@Transactional(readOnly = true)
public List<Order> getAllOrders() {
    return orderRepository.findAll();
}
```

**NPA (✅ Supported):**
```csharp
// Using EntityManager with transactions
public async Task CreateOrderAsync(Order order)
{
    using var transaction = _entityManager.BeginTransaction();
    try
    {
        await _entityManager.PersistAsync(order);
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}

// Read-only optimization
public async Task<IEnumerable<Order>> GetAllOrdersAsync()
{
    // Read-only operations don't need transactions
    return await _repository.GetAllAsync();
}
```

### 4.2 Entity Lifecycle States

**JPA Entity States:**
- **Transient**: New entity, not yet persisted
- **Managed**: Entity is tracked by persistence context
- **Detached**: Entity was managed but is no longer
- **Removed**: Entity is scheduled for deletion

**NPA Equivalent:**
- **Transient**: Entity with default/zero ID
- **Managed**: Entity loaded via EntityManager or Repository
- **Detached**: Entity after transaction ends (NPA doesn't maintain persistent context)
- **Removed**: Entity marked for deletion via `RemoveAsync()`

**NPA Behavior:**
```csharp
// Transient entity
var order = new Order { OrderNumber = "ORD-001" };
// order.Id == 0 (default)

// Persist (becomes managed)
await entityManager.PersistAsync(order);
// order.Id is now set by database

// After transaction ends, entity is detached
// Changes to detached entities are NOT automatically tracked

// To update, use MergeAsync
await entityManager.MergeAsync(order);
// Creates new managed instance with updated state
```

### 4.3 Automatic Dirty Checking

**JPA:**
```java
@Transactional
public void updateOrder(Long id) {
    Order order = orderRepository.findById(id).orElseThrow();
    order.setStatus(OrderStatus.COMPLETED);
    // JPA automatically detects change and issues UPDATE on commit
    // No explicit save() needed
}
```

**NPA Status:** ❌ Not implemented (no persistent context)

**NPA Approach:**
```csharp
// Explicit update required
public async Task UpdateOrderAsync(long id)
{
    var order = await _repository.GetByIdAsync(id);
    if (order == null) throw new NotFoundException();
    
    order.Status = OrderStatus.Completed;
    await _repository.UpdateAsync(order); // Explicit update
}

// Or use MergeAsync
public async Task UpdateOrderAsync(Order order)
{
    await _entityManager.MergeAsync(order); // Explicit merge
}
```

### 4.4 Read-Only Optimization

**JPA:**
```java
@Transactional(readOnly = true)
public List<Order> getAllCompletedOrders() {
    // JPA skips dirty checking for read-only transactions
    return orderRepository.findByStatus(OrderStatus.COMPLETED);
}
```

**NPA:**
```csharp
// NPA doesn't maintain persistent context, so read-only is implicit
// No special annotation needed - just don't call Persist/Merge/Remove
public async Task<IEnumerable<Order>> GetAllCompletedOrdersAsync()
{
    return await _repository.FindAsync(o => o.Status == OrderStatus.Completed);
}
```

### 4.5 Transaction Propagation

**JPA (Spring):**
```java
@Transactional(propagation = Propagation.REQUIRED) // Default
public void method1() { ... }

@Transactional(propagation = Propagation.REQUIRES_NEW)
public void method2() { ... }

@Transactional(propagation = Propagation.NESTED)
public void method3() { ... }
```

**NPA Status:** ⚠️ Basic support only

**NPA:**
```csharp
// Nested transactions supported via BeginTransaction
public async Task Method1Async()
{
    using var transaction = _entityManager.BeginTransaction();
    // ... operations ...
    await Method2Async(); // Uses same transaction
    await transaction.CommitAsync();
}

public async Task Method2Async()
{
    // Uses existing transaction if available
    // Or creates new one if called independently
}
```

---

## 5. Important Distinctions: NPA vs JPA

### 5.1 Entity Relationship Mapping vs Other JPA Features

NPA implements **Entity Relationship Mapping** which maps relationships between entities:
- Maps relationships between entities (Order ↔ Customer)
- Uses `@ManyToOne`, `@OneToMany`, `@OneToOne`, `@ManyToMany`
- Stores foreign keys in database tables
- Maintains object references in memory

### 5.2 JPA Features NOT Currently Implemented in NPA

#### Map Mapping (Element Collections)
- **Purpose**: Stores key-value pairs in a separate collection table
- **JPA Annotation**: `@ElementCollection` with `Map<String, String>`
- **Example**: `Map<String, String> departments` storing department codes and names
- **Status**: ❌ NOT implemented in NPA
- **Note**: This is for storing collections of basic types, NOT entity relationships
- **Workaround**: See [Element Collections](#24-element-collections-elementcollection) section above

#### Secondary Table Mapping
- **Purpose**: Maps multiple database tables to a single entity class
- **JPA Annotation**: `@SecondaryTable` and `@SecondaryTables`
- **Use Case**: When related fields are scattered across multiple tables but you want to model them as a single entity
- **Status**: ❌ NOT implemented in NPA
- **Workaround**: See [Secondary Table Mapping](#23-secondary-table-mapping-secondarytable) section above

#### Embedded/Embeddable Components
- **Purpose**: Maps a single table to multiple classes (opposite of SecondaryTable)
- **JPA Annotations**: `@Embedded` and `@Embeddable`
- **Use Case**: Logical grouping of fields within the same table
- **Status**: ❌ NOT implemented in NPA
- **Workaround**: See [Value Objects](#21-value-objects-embedded--embeddable) section above

### 5.3 Current NPA Approach for Multi-Table Scenarios

If you need to work with data from multiple tables, NPA currently requires:

**Option 1: Separate Entities with OneToOne**
```csharp
[Entity]
[Table("meal")]
public class Meal
{
    [Id]
    public long Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
    
    [OneToOne]
    [JoinColumn("meal_id")]
    public Allergens Allergens { get; set; }
}

[Entity]
[Table("allergens")]
public class Allergens
{
    [Id]
    [Column("meal_id")]
    public long MealId { get; set; }
    
    [OneToOne]
    [PrimaryKeyJoinColumn("meal_id")]
    public Meal Meal { get; set; }
    
    [Column("peanuts")]
    public bool Peanuts { get; set; }
}
```

**Option 2: Single Table (Denormalize)**
- Combine all fields into a single table
- Use `@Column` attributes to map all properties

**Option 3: Manual JOIN Queries**
- Use CPQL to manually join tables when querying
- Map results to DTOs or view models

---

## 6. Feature Comparison Matrix

| Feature | JPA | NPA Status | Notes |
|---------|-----|------------|-------|
| **Basic Entity Mapping** |
| `@Entity` | ✅ | ✅ | `[Entity]` |
| `@Table` | ✅ | ✅ | `[Table]` |
| `@Id` | ✅ | ✅ | `[Id]` |
| `@GeneratedValue` | ✅ | ✅ | `[GeneratedValue]` - Identity & Sequence supported |
| `@Column` | ✅ | ✅ | `[Column]` |
| **Relationships** |
| `@ManyToOne` | ✅ | ✅ | `[ManyToOne]` |
| `@OneToMany` | ✅ | ✅ | `[OneToMany]` |
| `@OneToOne` | ✅ | ✅ | `[OneToOne]` |
| `@ManyToMany` | ✅ | ✅ | `[ManyToMany]` |
| `@JoinColumn` | ✅ | ✅ | `[JoinColumn]` |
| `@JoinTable` | ✅ | ✅ | `[JoinTable]` |
| **Advanced Mapping** |
| `@Embedded` / `@Embeddable` | ✅ | ❌ | Use flat properties or composition |
| `@EmbeddedId` | ✅ | ✅ | Use multiple `[Id]` attributes |
| `@SecondaryTable` | ✅ | ❌ | Use `@OneToOne` with separate entity |
| `@ElementCollection` | ✅ | ❌ | Use `@OneToMany` with separate entity |
| `@Convert` | ✅ | ❌ | Manual conversion in properties |
| `@Inheritance` (JOINED) | ✅ | ❌ | Use separate entities with `@OneToOne` |
| `AttributeConverter` | ✅ | ❌ | Manual conversion in properties |
| **Repository Features** |
| `JpaRepository` | ✅ | ✅ | `IRepository<T, TKey>` |
| Query Method Derivation | ✅ | ❌ | Use `[NamedQuery]`, `[Query]`, or LINQ expressions |
| `@EntityGraph` | ✅ | ❌ | Use `JOIN FETCH` in CPQL or `Fetch = FetchType.Eager` |
| `@CreatedDate` / `@LastModifiedDate` | ✅ | ❌ | Manual implementation in base class or service methods |
| `@CreatedBy` / `@LastModifiedBy` | ✅ | ❌ | Manual implementation with `ICurrentUserService` |
| `@EnableJpaAuditing` | ✅ | ❌ | Manual audit field management |
| `@Query` (JPQL) | ✅ | ✅ | `[Query]` with CPQL |
| `@Query` (Native) | ✅ | ✅ | `[Query]` with `NativeQuery = true` |
| `@NamedQuery` | ✅ | ✅ | `[NamedQuery]` |
| `@Lock` | ✅ | ❌ | Use native SQL or transaction isolation |
| Paging & Sorting | ✅ | ✅ | LINQ-style methods |
| `JpaSpecificationExecutor` | ✅ | ❌ | Use LINQ expressions or CPQL builders |
| **Transactions** |
| `@Transactional` | ✅ | ✅ | `BeginTransaction()` / `CommitAsync()` |
| Read-Only Optimization | ✅ | ✅ | Implicit (no persistent context) |
| Propagation | ✅ | ⚠️ | Basic nested transaction support |
| Automatic Dirty Checking | ✅ | ❌ | Explicit `UpdateAsync()` or `MergeAsync()` |
| **Fetch Strategies** |
| `FetchType.LAZY` | ✅ | ✅ | `Fetch = FetchType.Lazy` |
| `FetchType.EAGER` | ✅ | ✅ | `Fetch = FetchType.Eager` |
| `JOIN FETCH` | ✅ | ✅ | CPQL supports JOIN |
| **Cascade Operations** |
| `CascadeType.ALL` | ✅ | ✅ | `Cascade = CascadeType.All` |
| `CascadeType.PERSIST` | ✅ | ✅ | `Cascade = CascadeType.Persist` |
| `CascadeType.REMOVE` | ✅ | ✅ | `Cascade = CascadeType.Remove` |
| `CascadeType.MERGE` | ✅ | ✅ | `Cascade = CascadeType.Merge` |
| `orphanRemoval` | ✅ | ✅ | `OrphanRemoval = true` |

---

## 7. Best Practices

### 6.1 Entity Design

1. **Use navigation properties only** - Foreign key properties are optional
2. **Lazy loading by default** - Use `Fetch = FetchType.Eager` only when needed
3. **Cascade carefully** - Only cascade operations you truly need
4. **Use `nameof()` for MappedBy** - Prevents typos: `MappedBy = nameof(Order.Customer)`

### 6.2 Repository Design

1. **Use `[NamedQuery]` for complex queries** - Better than inline SQL
2. **Leverage generated repositories** - Let the generator create CRUD methods
3. **Custom methods for business logic** - Implement in repository implementation
4. **Use CPQL for type-safe queries** - Similar to JPQL but C#-native

### 6.3 Transaction Management

1. **Explicit transactions for writes** - Always use transactions for Persist/Update/Remove
2. **No transactions for reads** - Read operations don't need transactions
3. **Handle exceptions** - Always rollback on error
4. **Use async methods** - Prefer `Async` methods for better performance

### 6.4 Performance

1. **Eager loading with JOIN FETCH** - Prevents N+1 queries
2. **Batch operations** - Use `BulkInsertAsync`, `BulkUpdateAsync` for large datasets
3. **Caching** - Use `ICacheProvider` for frequently accessed data
4. **Connection pooling** - Configured automatically by NPA

---

## 8. Migration Guide: JPA to NPA

### Converting JPA Entities

1. **Change annotations**: `@Entity` → `[Entity]`, `@Table` → `[Table]`, etc.
2. **Property names**: Java camelCase → C# PascalCase
3. **Collections**: `Set<T>` → `ICollection<T>` or `List<T>`
4. **Nullable types**: Use C# nullable reference types (`Customer?`)

### Converting Repositories

1. **Interface inheritance**: `JpaRepository<T, ID>` → `IRepository<T, TKey>`
2. **Query methods**: Convert method names to `[NamedQuery]` or `[Query]` attributes
3. **JPQL to CPQL**: Convert Java property names to C# property names
4. **Return types**: `Optional<T>` → `Task<T?>`, `List<T>` → `Task<IEnumerable<T>>`

### Converting Services

1. **Transaction management**: `@Transactional` → `BeginTransaction()` / `CommitAsync()`
2. **Explicit updates**: Add `UpdateAsync()` or `MergeAsync()` calls
3. **Async methods**: Convert to `async Task` methods
4. **Dependency injection**: Use NPA's DI extensions

---

## 9. Future Enhancements

### Planned Features

- [ ] `@Embedded` / `@Embeddable` support
- [ ] `@SecondaryTable` support
- [ ] `@ElementCollection` support
- [ ] `@Inheritance` support (JOINED, SINGLE_TABLE, TABLE_PER_CLASS strategies)
- [ ] `AttributeConverter` for custom type conversion
- [ ] Query method derivation (method name parsing)
- [ ] `@Lock` annotation support
- [ ] Persistent context with automatic dirty checking
- [ ] `@BatchSize` for collection batching
- [ ] Specification pattern support (Criteria API equivalent)

### Under Consideration

- [ ] Entity listeners (`@PrePersist`, `@PostLoad`, etc.)
- [ ] Polymorphic queries
- [ ] Advanced inheritance strategies (beyond JOINED)

---

## Conclusion

NPA provides a comprehensive JPA-like experience for .NET developers, with strong support for core entity mapping, relationships, repositories, and transactions. While some advanced features like `@Embedded`, `@SecondaryTable`, and query method derivation are not yet implemented, NPA offers workarounds and alternative approaches that maintain similar functionality.

The framework prioritizes performance (built on Dapper), type safety, and developer productivity while maintaining familiar JPA patterns for developers transitioning from Java to .NET.

