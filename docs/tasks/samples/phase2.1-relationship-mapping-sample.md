# Phase 2.1: Relationship Mapping Sample

> **⚠️ PLANNED FEATURE**: This sample describes functionality planned for Phase 2.1. Relationship mapping is not yet implemented in NPA. This document serves as a design reference and future implementation guide.

## 📋 Task Overview

**Objective**: Create a sample application demonstrating entity relationships (One-to-Many, Many-to-One, Many-to-Many).

**Priority**: High  
**Estimated Time**: 6-8 hours  
**Dependencies**: Phase 2.1 (Relationship Mapping) - **NOT YET IMPLEMENTED**  
**Target Framework**: .NET 8.0  
**Sample Name**: RelationshipMappingSample  
**Status**: 📋 Planned for Phase 2

## 🎯 Success Criteria

- [ ] Demonstrates One-to-Many relationships
- [ ] Demonstrates Many-to-One relationships
- [ ] Demonstrates Many-to-Many relationships
- [ ] Shows proper navigation properties
- [ ] Includes join column configurations
- [ ] Uses join tables for Many-to-Many
- [ ] Demonstrates lazy and eager loading
- [ ] Shows cascade operations

## 📝 Detailed Requirements

### 1. Entity Relationships

**One-to-Many**: Author has many Books
**Many-to-One**: Book belongs to one Author
**Many-to-Many**: Book has many Categories, Category has many Books

### 2. Features to Demonstrate

- Navigation properties
- Foreign key mappings
- Join tables
- Bidirectional relationships
- Cascade types
- Lazy loading proxies

## 🏗️ Entity Design

```csharp
// Author (One-to-Many with Books)
[Entity]
[Table("authors")]
public class Author
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("name", Length = 100, IsNullable = false)]
    public string Name { get; set; }
    
    // One-to-Many relationship
    [OneToMany(MappedBy = "Author", Cascade = CascadeType.All)]
    public ICollection<Book> Books { get; set; } = new List<Book>();
}

// Book (Many-to-One with Author, Many-to-Many with Categories)
[Entity]
[Table("books")]
public class Book
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("title", Length = 200, IsNullable = false)]
    public string Title { get; set; }
    
    // Many-to-One relationship
    [ManyToOne]
    [JoinColumn("author_id")]
    public Author Author { get; set; }
    
    // Many-to-Many relationship
    [ManyToMany]
    [JoinTable("book_categories",
        JoinColumns = "book_id",
        InverseJoinColumns = "category_id")]
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}

// Category (Many-to-Many with Books)
[Entity]
[Table("categories")]
public class Category
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public long Id { get; set; }
    
    [Column("name", Length = 50, IsNullable = false)]
    public string Name { get; set; }
    
    [ManyToMany(MappedBy = "Categories")]
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
```

## 💻 Operations to Demonstrate

1. **Create Author with Books** (cascade persist)
2. **Load Author with Books** (eager loading)
3. **Add Category to Book** (Many-to-Many)
4. **Load Book with Categories** (join table query)
5. **Update through relationships**
6. **Delete with cascade**

## 📚 Learning Outcomes

- Understanding of relationship types
- Navigation property usage
- Join strategies
- Cascade operations
- Lazy vs eager loading
- Join table management

---

*Created: October 8, 2025*  
*Status: ⏳ Pending*
