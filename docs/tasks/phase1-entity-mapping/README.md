# Phase 1.1: Basic Entity Mapping with Attributes

## 📋 Task Overview

**Objective**: Implement basic entity mapping attributes that provide JPA-like functionality for .NET entities.

**Priority**: High  
**Estimated Time**: 2-3 days  
**Dependencies**: None  
**Target Framework**: .NET 6.0  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] All entity mapping attributes are implemented
- [ ] Attributes follow JPA conventions
- [ ] Unit tests cover all attribute functionality
- [ ] Documentation is complete
- [ ] Code follows .NET best practices

## 📝 Detailed Requirements

### 1. EntityAttribute
- **Purpose**: Marks a class as an entity
- **Usage**: `[Entity]` on class declaration
- **Properties**: None (marker attribute)
- **Validation**: Class must be public and non-abstract

### 2. TableAttribute
- **Purpose**: Specifies the database table name
- **Usage**: `[Table("table_name")]` on class
- **Properties**:
  - `Name` (string): Table name
  - `Schema` (string, optional): Schema name
- **Validation**: Table name must not be null or empty

### 3. IdAttribute
- **Purpose**: Marks a property as primary key
- **Usage**: `[Id]` on property
- **Properties**: None (marker attribute)
- **Validation**: Property must be public and have getter/setter

### 4. ColumnAttribute
- **Purpose**: Maps property to database column
- **Usage**: `[Column("column_name")]` on property
- **Properties**:
  - `Name` (string): Column name
  - `TypeName` (string, optional): Database type
  - `Length` (int, optional): Column length
  - `Precision` (int, optional): Numeric precision
  - `Scale` (int, optional): Numeric scale
  - `IsNullable` (bool, optional): Nullable flag
  - `IsUnique` (bool, optional): Unique constraint
- **Validation**: Column name must not be null or empty

### 5. GeneratedValueAttribute
- **Purpose**: Specifies how primary key is generated
- **Usage**: `[GeneratedValue(GenerationType.Identity)]` on Id property
- **Properties**:
  - `Strategy` (GenerationType): Generation strategy
  - `Generator` (string, optional): Generator name
- **Validation**: Must be used on Id properties only

### 6. GenerationType Enum
- **Purpose**: Defines primary key generation strategies
- **Values**:
  - `Identity`: Database auto-increment
  - `Sequence`: Database sequence
  - `Table`: Table-based generation
  - `None`: No generation (manual assignment)

## 🏗️ Implementation Plan

### Step 1: Create Attribute Classes
1. Create `EntityAttribute.cs`
2. Create `TableAttribute.cs`
3. Create `IdAttribute.cs`
4. Create `ColumnAttribute.cs`
5. Create `GeneratedValueAttribute.cs`
6. Create `GenerationType.cs`

### Step 2: Add Validation Logic
1. Implement attribute validation
2. Add error handling
3. Create validation exceptions

### Step 3: Create Unit Tests
1. Test attribute application
2. Test validation logic
3. Test error scenarios
4. Test edge cases

### Step 4: Add Documentation
1. XML documentation comments
2. Usage examples
3. Best practices guide

## 📁 File Structure

```
src/NPA.Core/Annotations/
├── EntityAttribute.cs
├── TableAttribute.cs
├── IdAttribute.cs
├── ColumnAttribute.cs
├── GeneratedValueAttribute.cs
└── GenerationType.cs

tests/NPA.Core.Tests/Annotations/
├── EntityAttributeTests.cs
├── TableAttributeTests.cs
├── IdAttributeTests.cs
├── ColumnAttributeTests.cs
└── GeneratedValueAttributeTests.cs
```

## 💻 Code Examples

### EntityAttribute
```csharp
[AttributeUsage(AttributeTargets.Class)]
public class EntityAttribute : Attribute
{
    // Marker attribute - no properties needed
}
```

### TableAttribute
```csharp
[AttributeUsage(AttributeTargets.Class)]
public class TableAttribute : Attribute
{
    public string Name { get; }
    public string? Schema { get; set; }
    
    public TableAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
```

### ColumnAttribute
```csharp
[AttributeUsage(AttributeTargets.Property)]
public class ColumnAttribute : Attribute
{
    public string Name { get; }
    public string? TypeName { get; set; }
    public int? Length { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsNullable { get; set; } = true;
    public bool IsUnique { get; set; } = false;
    
    public ColumnAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
```

## 🧪 Test Cases

### EntityAttribute Tests
- [ ] Valid class application
- [ ] Invalid application (non-class)
- [ ] Multiple applications (should be allowed)

### TableAttribute Tests
- [ ] Valid table name
- [ ] Null table name (should throw)
- [ ] Empty table name (should throw)
- [ ] Schema specification
- [ ] Multiple applications (should be allowed)

### IdAttribute Tests
- [ ] Valid property application
- [ ] Invalid application (non-property)
- [ ] Multiple applications (should be allowed)

### ColumnAttribute Tests
- [ ] Valid column name
- [ ] Null column name (should throw)
- [ ] Empty column name (should throw)
- [ ] All optional properties
- [ ] Validation logic

### GeneratedValueAttribute Tests
- [ ] Valid strategy application
- [ ] Invalid application (non-Id property)
- [ ] All generation types
- [ ] Generator name specification

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Basic entity mapping
- [ ] Table mapping
- [ ] Column mapping
- [ ] Primary key generation
- [ ] Best practices

## 🔍 Code Review Checklist

- [ ] Code follows .NET naming conventions
- [ ] All public members have XML documentation
- [ ] Error handling is appropriate
- [ ] Unit tests cover all scenarios
- [ ] Code is readable and maintainable
- [ ] Performance considerations addressed
- [ ] Security considerations addressed

## 🚀 Next Steps

After completing this task:
1. Move to Phase 1.2: EntityManager with CRUD Operations
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on attribute validation
- [ ] Performance considerations for attribute reflection
- [ ] Integration with existing .NET attributes
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
