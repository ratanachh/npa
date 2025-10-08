# Phase 1.1: Basic Entity Mapping Sample

## 📋 Task Overview

**Objective**: Create a simple console application demonstrating basic entity mapping with NPA attributes.

**Priority**: High  
**Estimated Time**: 2-3 hours  
**Dependencies**: Phase 1.1 (Basic Entity Mapping with Attributes)  
**Target Framework**: .NET 8.0  
**Sample Name**: BasicEntityMapping

## 🎯 Success Criteria

- [ ] Sample application compiles and runs successfully
- [ ] Demonstrates all entity mapping attributes
- [ ] Shows proper attribute usage patterns
- [ ] Includes clear console output
- [ ] README explains concepts
- [ ] Code is well-commented
- [ ] Follows .NET best practices

## 📝 Detailed Requirements

### 1. Entity Classes

Create entity classes demonstrating:
- `[Entity]` attribute usage
- `[Table]` with custom table names and schemas
- `[Id]` for primary key marking
- `[Column]` with various configurations
- `[GeneratedValue]` with different strategies

### 2. Console Application

Implement console app that:
- Creates entity instances
- Displays entity metadata
- Shows attribute values
- Demonstrates metadata reflection
- Provides clear output formatting

### 3. Example Entities

Create at least 3 entity classes:
- **User** - Basic entity with identity generation
- **Product** - Entity with custom column mappings
- **Order** - Entity with schema specification

## 🏗️ Implementation Plan

### Step 1: Project Setup
```bash
dotnet new console -n BasicEntityMapping
cd BasicEntityMapping
dotnet add reference ../../src/NPA.Core/NPA.Core.csproj
```

### Step 2: Create Entity Classes

**User.cs**
```csharp
using NPA.Core.Annotations;

namespace BasicEntityMapping.Entities;

[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("id")]
    public long Id { get; set; }

    [Column("username", Length = 50, IsNullable = false, IsUnique = true)]
    public string Username { get; set; } = string.Empty;

    [Column("email", Length = 255, IsNullable = false)]
    public string Email { get; set; } = string.Empty;

    [Column("created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("is_active", IsNullable = false)]
    public bool IsActive { get; set; } = true;
}
```

**Product.cs**
```csharp
using NPA.Core.Annotations;

namespace BasicEntityMapping.Entities;

[Entity]
[Table("products", Schema = "catalog")]
public class Product
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("product_name", Length = 200, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [Column("description", TypeName = "TEXT")]
    public string? Description { get; set; }

    [Column("price", Precision = 18, Scale = 2, IsNullable = false)]
    public decimal Price { get; set; }

    [Column("stock_quantity", IsNullable = false)]
    public int StockQuantity { get; set; }

    [Column("created_date", IsNullable = false)]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
```

**Order.cs**
```csharp
using NPA.Core.Annotations;

namespace BasicEntityMapping.Entities;

[Entity]
[Table("orders", Schema = "sales")]
public class Order
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("order_id")]
    public long OrderId { get; set; }

    [Column("order_number", Length = 50, IsNullable = false, IsUnique = true)]
    public string OrderNumber { get; set; } = string.Empty;

    [Column("customer_id", IsNullable = false)]
    public long CustomerId { get; set; }

    [Column("order_date", IsNullable = false)]
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Column("total_amount", Precision = 18, Scale = 2, IsNullable = false)]
    public decimal TotalAmount { get; set; }

    [Column("status", Length = 20, IsNullable = false)]
    public string Status { get; set; } = "Pending";
}
```

### Step 3: Implement Metadata Display

**MetadataInspector.cs**
```csharp
using System.Reflection;
using NPA.Core.Annotations;

namespace BasicEntityMapping;

public static class MetadataInspector
{
    public static void DisplayEntityMetadata<T>() where T : class
    {
        var type = typeof(T);
        
        Console.WriteLine($"\n{'='*60}");
        Console.WriteLine($"Entity: {type.Name}");
        Console.WriteLine($"{'='*60}");

        // Display Entity attribute
        var entityAttr = type.GetCustomAttribute<EntityAttribute>();
        Console.WriteLine($"[Entity] attribute: {(entityAttr != null ? "✓ Present" : "✗ Missing")}");

        // Display Table attribute
        var tableAttr = type.GetCustomAttribute<TableAttribute>();
        if (tableAttr != null)
        {
            Console.WriteLine($"[Table] Name: {tableAttr.Name}");
            if (!string.IsNullOrEmpty(tableAttr.Schema))
                Console.WriteLine($"[Table] Schema: {tableAttr.Schema}");
        }

        Console.WriteLine($"\nProperties:");
        Console.WriteLine($"{'-'*60}");

        // Display properties and their attributes
        foreach (var prop in type.GetProperties())
        {
            Console.WriteLine($"\n  Property: {prop.Name} ({prop.PropertyType.Name})");

            var idAttr = prop.GetCustomAttribute<IdAttribute>();
            if (idAttr != null)
            {
                Console.WriteLine($"    [Id] ✓ Primary Key");
            }

            var genAttr = prop.GetCustomAttribute<GeneratedValueAttribute>();
            if (genAttr != null)
            {
                Console.WriteLine($"    [GeneratedValue] Strategy: {genAttr.Strategy}");
            }

            var colAttr = prop.GetCustomAttribute<ColumnAttribute>();
            if (colAttr != null)
            {
                Console.WriteLine($"    [Column] Name: {colAttr.Name}");
                if (colAttr.Length.HasValue)
                    Console.WriteLine($"    [Column] Length: {colAttr.Length}");
                if (colAttr.Precision.HasValue)
                    Console.WriteLine($"    [Column] Precision: {colAttr.Precision}");
                if (colAttr.Scale.HasValue)
                    Console.WriteLine($"    [Column] Scale: {colAttr.Scale}");
                if (!string.IsNullOrEmpty(colAttr.TypeName))
                    Console.WriteLine($"    [Column] TypeName: {colAttr.TypeName}");
                Console.WriteLine($"    [Column] Nullable: {colAttr.IsNullable}");
                Console.WriteLine($"    [Column] Unique: {colAttr.IsUnique}");
            }
        }

        Console.WriteLine($"\n{'='*60}\n");
    }

    public static void DisplayEntityInstance<T>(T entity, string title) where T : class
    {
        var type = typeof(T);
        
        Console.WriteLine($"\n{title}");
        Console.WriteLine($"{'-'*60}");

        foreach (var prop in type.GetProperties())
        {
            var value = prop.GetValue(entity);
            Console.WriteLine($"  {prop.Name}: {value}");
        }

        Console.WriteLine();
    }
}
```

### Step 4: Implement Main Program

**Program.cs**
```csharp
using BasicEntityMapping;
using BasicEntityMapping.Entities;

Console.WriteLine("NPA - Basic Entity Mapping Sample");
Console.WriteLine("==================================\n");

// Display metadata for each entity
Console.WriteLine("Part 1: Entity Metadata Inspection");
MetadataInspector.DisplayEntityMetadata<User>();
MetadataInspector.DisplayEntityMetadata<Product>();
MetadataInspector.DisplayEntityMetadata<Order>();

// Create sample instances
Console.WriteLine("\nPart 2: Entity Instance Creation");

var user = new User
{
    Username = "john_doe",
    Email = "john.doe@example.com",
    CreatedAt = DateTime.UtcNow,
    IsActive = true
};

var product = new Product
{
    Name = "Laptop Computer",
    Description = "High-performance laptop with 16GB RAM",
    Price = 1299.99m,
    StockQuantity = 50,
    CreatedDate = DateTime.UtcNow
};

var order = new Order
{
    OrderNumber = "ORD-2024-001",
    CustomerId = 1,
    OrderDate = DateTime.UtcNow,
    TotalAmount = 1299.99m,
    Status = "Pending"
};

// Display instances
MetadataInspector.DisplayEntityInstance(user, "User Instance:");
MetadataInspector.DisplayEntityInstance(product, "Product Instance:");
MetadataInspector.DisplayEntityInstance(order, "Order Instance:");

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
```

### Step 5: Create README

**README.md**
```markdown
# Basic Entity Mapping Sample

This sample demonstrates how to use NPA entity mapping attributes to define database entities in .NET.

## Features Demonstrated

- Entity marking with `[Entity]` attribute
- Table mapping with `[Table]` attribute (with schema support)
- Primary key definition with `[Id]` attribute
- Column mapping with `[Column]` attribute
- Auto-generated values with `[GeneratedValue]` attribute
- Various column configurations (length, precision, nullable, unique)

## Running the Sample

```bash
cd samples/BasicEntityMapping
dotnet run
```

## Key Concepts

### Entity Attribute
Marks a class as a database entity that NPA will manage.

### Table Attribute
Specifies the database table name and optionally the schema for an entity.

### Id Attribute
Marks a property as the primary key of the entity.

### Column Attribute
Maps a property to a specific database column with custom configurations.

### GeneratedValue Attribute
Specifies how the primary key value is generated (e.g., Identity, Sequence).

## Expected Output

The application will display:
1. Metadata for each entity (User, Product, Order)
2. Attribute information for each property
3. Sample instances of each entity

## Learn More

- [Phase 1.1 Documentation](../../docs/tasks/phase1.1-basic-entity-mapping-with-attributes/README.md)
- [NPA Core Documentation](../../README.md)
```

### Step 6: Create .csproj

**BasicEntityMapping.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>10.0</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\NPA.Core\NPA.Core.csproj" />
  </ItemGroup>

</Project>
```

## 📁 Project Structure

```
samples/BasicEntityMapping/
├── BasicEntityMapping.csproj
├── Program.cs
├── MetadataInspector.cs
├── README.md
└── Entities/
    ├── User.cs
    ├── Product.cs
    └── Order.cs
```

## 🧪 Test Cases

### Manual Testing
- [ ] Application compiles without errors
- [ ] All entity metadata displays correctly
- [ ] Entity attributes are properly shown
- [ ] Column configurations are accurate
- [ ] Console output is clear and formatted
- [ ] No runtime exceptions occur

## 📚 Documentation Requirements

- [x] Inline code comments
- [x] XML documentation for public members
- [x] README with usage instructions
- [x] Explanation of each attribute
- [x] Expected output description

## 🎓 Learning Outcomes

After completing this sample, developers will understand:
- How to define entities using NPA attributes
- Table and column mapping configuration
- Primary key generation strategies
- Entity metadata inspection
- Attribute-based ORM concepts

## 🔗 Related Samples

- Phase 1.2 - CRUD Operations Sample
- Phase 1.3 - Query API Sample
- Phase 2.1 - Relationship Mapping Sample

---

*Created: October 8, 2025*  
*Status: ⏳ Pending*
