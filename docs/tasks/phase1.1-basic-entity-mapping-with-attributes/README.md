# Phase 1.1: Basic Entity Mapping with Attributes

## 📋 Task Overview

**Objective**: Implement basic entity mapping attributes that provide JPA-like functionality for .NET entities.

**Priority**: High  
**Estimated Time**: 2-3 days  
**Dependencies**: None  
**Target Framework**: .NET 8.0  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [x] All entity mapping attributes are implemented
- [x] Attributes follow JPA conventions
- [x] Unit tests cover all attribute functionality
- [x] Documentation is complete
- [x] Code follows .NET best practices

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
- [x] Valid class application
- [x] Invalid application (non-class)
- [x] Multiple applications (should be allowed)

### TableAttribute Tests
- [x] Valid table name
- [x] Null table name (should throw)
- [x] Empty table name (should throw)
- [x] Schema specification
- [x] Multiple applications (should be allowed)

### IdAttribute Tests
- [x] Valid property application
- [x] Invalid application (non-property)
- [x] Multiple applications (should be allowed)

### ColumnAttribute Tests
- [x] Valid column name
- [x] Null column name (should throw)
- [x] Empty column name (should throw)
- [x] All optional properties
- [x] Validation logic

### GeneratedValueAttribute Tests
- [x] Valid strategy application
- [x] Invalid application (non-Id property)
- [x] All generation types
- [x] Generator name specification

## 📚 Documentation Requirements

### XML Documentation
- [x] All public members documented
- [x] Parameter descriptions
- [x] Return value descriptions
- [x] Exception documentation
- [x] Usage examples

### Usage Guide
- [x] Basic entity mapping
- [x] Table mapping
- [x] Column mapping
- [x] Primary key generation
- [x] Best practices

## 🔍 Code Review Checklist

- [x] Code follows .NET naming conventions
- [x] All public members have XML documentation
- [x] Error handling is appropriate
- [x] Unit tests cover all scenarios
- [x] Code is readable and maintainable
- [x] Performance considerations addressed
- [x] Security considerations addressed

## 🚀 Next Steps

After completing this task:
1. Move to Phase 1.2: EntityManager with CRUD Operations
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [x] Clarification needed on attribute validation - **RESOLVED**: Implemented proper validation with ArgumentException
- [x] Performance considerations for attribute reflection - **RESOLVED**: Using efficient reflection patterns
- [x] Integration with existing .NET attributes - **RESOLVED**: Properly integrated with .NET attribute system
- [x] Error message localization - **RESOLVED**: Using standard .NET exception messages

## ✅ Implementation Notes

### Completed Features
- All 6 entity mapping attributes implemented with full XML documentation
- Comprehensive unit test coverage for all attributes
- Proper validation and error handling
- JPA-compliant attribute design
- .NET 8.0 compatibility

### Test Coverage
- **EntityAttributeTests.cs**: Tests for entity marking functionality
- **TableAttributeTests.cs**: Tests for table name and schema mapping
- **IdAttributeTests.cs**: Tests for primary key marking (newly added)
- **ColumnAttributeTests.cs**: Tests for column mapping with all properties
- **GeneratedValueAttributeTests.cs**: Tests for primary key generation strategies

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: ✅ COMPLETED*
