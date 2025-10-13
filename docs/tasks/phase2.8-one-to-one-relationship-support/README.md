# Phase 2.8: One-to-One Relationship Support

## 📋 Task Overview

**Objective**: Implement support for `OneToOne` relationships, a feature that was identified as missing during a code review. This completes the core relationship mapping capabilities of the framework.

**Priority**: High  
**Status**: ✅ COMPLETED

## 🎯 Success Criteria

- [x] `OneToOneAttribute.cs` is created and implemented.
- [x] Metadata providers (reflection and source-generated) are updated to process the new attribute.
- [x] `SqlGenerator` is updated to correctly generate `JOIN` clauses for `OneToOne` relationships.
- [x] Unit tests are created for the attribute, metadata processing, and SQL generation.
- [x] Documentation is updated to include `OneToOne` relationships.

## 📝 Detailed Requirements

### 1. OneToOneAttribute
- **Purpose**: Defines a one-to-one association to another entity.
- **Usage**: `[OneToOne]` on a property.
- **Properties**:
  - `MappedBy` (string, optional): Specifies the property on the target entity that owns the relationship (for bidirectional associations).
  - `Cascade` (CascadeType, optional): Defines which operations should be cascaded.
  - `Fetch` (FetchType, optional): Defines the fetch strategy (defaults to `Eager`).
  - `Optional` (bool, optional): Defines if the relationship is nullable (defaults to `true`).

### 2. Metadata and SQL Generation
- **MetadataProvider**: The reflection-based provider was updated to recognize and build metadata for `[OneToOne]`.
- **EntityMetadataGenerator**: The source generator was updated to recognize and generate metadata for `[OneToOne]`.
- **SqlGenerator**: The `GenerateJoinClause` method was updated to handle `OneToOne` relationships, correctly identifying the owning and inverse sides to generate the proper `ON` condition.

## 🏗️ Implementation Plan

### Step 1: Create the OneToOneAttribute ✅
1. Created the `src/NPA.Core/Annotations/OneToOneAttribute.cs` file.
2. Implemented the properties (`MappedBy`, `Cascade`, `Fetch`, `Optional`).

### Step 2: Update Metadata Processing ✅
1. Modified `MetadataProvider.cs` to handle `OneToOneAttribute`.
2. Modified `EntityMetadataGenerator.cs` to parse `OneToOneAttribute`.

### Step 3: Update SQL Generation ✅
1. Modified `SqlGenerator.cs` in the `GenerateJoinClause` method.
2. Added a case for `RelationshipType.OneToOne` to generate the correct `JOIN` and `ON` clause logic.

### Step 4: Create Unit Tests ✅
1. Added new tests to `tests/NPA.Core.Tests/Query/CPQL/SqlGeneratorTests.cs` to verify correct SQL generation for `OneToOne` joins.

### Step 5: Update Documentation ✅
1. This document has been updated to reflect the completion of the task.

## 📁 File Structure

```
src/NPA.Core/Annotations/
└── OneToOneAttribute.cs              (NEW)

tests/NPA.Core.Tests/Query/CPQL/
└── SqlGeneratorTests.cs              (MODIFIED)
```

## 💻 Code Examples

### Unidirectional One-to-One (Owning Side)
```csharp
// An Employee has one EmployeeProfile
// The 'employee_profiles' table will have a foreign key 'employee_id'.
[Entity]
[Table("employees")]
public class Employee
{
    [Id]
    public long Id { get; set; }

    [OneToOne]
    [JoinColumn("profile_id")]
    public EmployeeProfile Profile { get; set; }
}

[Entity]
[Table("employee_profiles")]
public class EmployeeProfile
{
    [Id]
    public long Id { get; set; }
    // ... other properties
}
```

### Bidirectional One-to-One
```csharp
[Entity]
[Table("users")]
public class User
{
    [Id]
    public long Id { get; set; }

    // The "MappedBy" tells NPA that the 'User' property on the 'Address'
    // entity defines the foreign key. This is the inverse side.
    [OneToOne(MappedBy = "User")]
    public Address Address { get; set; }
}

[Entity]
[Table("addresses")]
public class Address
{
    [Id]
    public long Id { get; set; }

    // This is the owning side. It has the [JoinColumn].
    [OneToOne]
    [JoinColumn("user_id")]
    public User User { get; set; }
}
```

## 🧪 Test Cases

- **Attribute Tests**: Validate `MappedBy`, `Cascade`, `Fetch`, and `Optional` properties on `OneToOneAttribute`.
- **SQL Generation Tests**:
  - `Generate_JoinWithOneToOne_OwningSide_ShouldGenerateCorrectSql`
  - `Generate_JoinWithOneToOne_InverseSide_ShouldGenerateCorrectSql`
- **Metadata Tests**: Ensure both reflection and source-generated providers create the correct `RelationshipMetadata` for `OneToOne`.

## 🚀 Next Steps

After completing this task, all primary relationship types (`OneToOne`, `ManyToOne`, `OneToMany`, `ManyToMany`) are now fully supported, making the framework's mapping capabilities complete.
