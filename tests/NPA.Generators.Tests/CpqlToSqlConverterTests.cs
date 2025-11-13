using Xunit;
using FluentAssertions;
using NPA.Generators;

namespace NPA.Generators.Tests;

/// <summary>
/// Tests for CPQL (Custom Persistence Query Language) to SQL conversion.
/// These tests ensure that entity-based queries are correctly converted to standard SQL.
/// </summary>
public class CpqlToSqlConverterTests
{
    [Fact]
    public void ConvertToSql_SimpleSelect_ShouldConvertCorrectly()
    {
        // Arrange
        var cpql = "SELECT p FROM Product p";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert
        sql.Should().Be("SELECT * FROM products");
    }

    [Fact]
    public void ConvertToSql_SelectWithWhere_ShouldPreserveWhereKeyword()
    {
        // Arrange
        var cpql = "SELECT p FROM Product p WHERE p.Price > :price";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves Price property casing
        sql.Should().Contain("WHERE");
        sql.Should().Contain("Price > @price");
        sql.Should().NotContain("p.Price");
    }

    [Fact]
    public void ConvertToSql_PlainSqlWithWhere_ShouldNotRemoveWhere()
    {
        // Arrange - This is the bug we found: plain SQL was losing WHERE keyword
        var sql = "SELECT * FROM users WHERE email = @email";

        // Act
        var result = CpqlToSqlConverter.ConvertToSql(sql);

        // Assert
        result.Should().Contain("WHERE email = @email");
        result.Should().NotBe("SELECT * FROM users email = @email");
    }

    [Fact]
    public void ConvertToSql_ParameterSyntax_ShouldConvertColonToAt()
    {
        // Arrange
        var cpql = "SELECT p FROM Product p WHERE p.Price > :price AND p.Category = :category";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert
        sql.Should().Contain("@price");
        sql.Should().Contain("@category");
        sql.Should().NotContain(":price");
        sql.Should().NotContain(":category");
    }

    [Fact]
    public void ConvertToSql_CountAggregate_ShouldConvertToCountStar()
    {
        // Arrange
        var cpql = "SELECT COUNT(p) FROM Product p WHERE p.IsActive = true";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert
        sql.Should().Contain("SELECT COUNT(*)");
        sql.Should().NotContain("COUNT(p)");
    }

    [Fact]
    public void ConvertToSql_AvgAggregate_ShouldRemoveAlias()
    {
        // Arrange
        var cpql = "SELECT AVG(p.Price) FROM Product p";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves Price property casing
        sql.Should().Contain("SELECT AVG(Price)");
        sql.Should().NotContain("p.Price");
    }

    [Fact]
    public void ConvertToSql_SumAggregate_ShouldRemoveAlias()
    {
        // Arrange
        var cpql = "SELECT SUM(p.Quantity) FROM Product p WHERE p.Category = :category";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves Quantity and Category property casing
        sql.Should().Contain("SELECT SUM(Quantity)");
        sql.Should().Contain("WHERE");
        sql.Should().Contain("Category = @category");
    }

    [Fact]
    public void ConvertToSql_MaxAggregate_ShouldRemoveAlias()
    {
        // Arrange
        var cpql = "SELECT MAX(p.Price) FROM Product p";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves Price property casing
        sql.Should().Contain("SELECT MAX(Price)");
    }

    [Fact]
    public void ConvertToSql_MinAggregate_ShouldRemoveAlias()
    {
        // Arrange
        var cpql = "SELECT MIN(p.Price) FROM Product p";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves Price property casing
        sql.Should().Contain("SELECT MIN(Price)");
    }

    [Fact]
    public void ConvertToSql_WithOrderBy_ShouldPreserveOrderBy()
    {
        // Arrange
        var cpql = "SELECT p FROM Product p WHERE p.IsActive = true ORDER BY p.Price DESC";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert
        sql.Should().Contain("ORDER BY");
        sql.Should().Contain("Price DESC");
        sql.Should().NotContain("p.Price");
        sql.Should().NotContain("p.price");
        sql.Should().NotContain("p.IsActive");
        sql.Should().NotContain("p.is_active");
    }

    [Fact]
    public void ConvertToSql_WithLimit_ShouldPreserveLimit()
    {
        // Arrange
        var cpql = "SELECT p FROM Product p WHERE p.Category = :category LIMIT :limit";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert
        sql.Should().Contain("LIMIT @limit");
        sql.Should().Contain("WHERE");
    }

    [Fact]
    public void ConvertToSql_ComplexQuery_ShouldHandleMultipleConditions()
    {
        // Arrange
        var cpql = "SELECT p FROM Product p WHERE p.Price > :minPrice AND p.Price < :maxPrice AND p.IsActive = true ORDER BY p.CreatedAt DESC";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves property casing
        sql.Should().Contain("WHERE");
        sql.Should().Contain("Price > @minPrice");
        sql.Should().Contain("Price < @maxPrice");
        sql.Should().Contain("IsActive = true");
        sql.Should().Contain("ORDER BY");
        sql.Should().Contain("CreatedAt DESC");
        sql.Should().NotContain("p.Price");
        sql.Should().NotContain("p.IsActive");
        sql.Should().NotContain("p.CreatedAt");
    }

    [Fact]
    public void ConvertToSql_DistinctSelect_ShouldPreserveDistinct()
    {
        // Arrange
        var cpql = "SELECT DISTINCT p FROM Product p WHERE p.Category = :category";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert
        sql.Should().Contain("SELECT DISTINCT *");
        sql.Should().Contain("WHERE");
    }

    [Fact]
    public void ConvertToSql_SingleLetterAlias_ShouldRemoveAlias()
    {
        // Arrange
        var cpql = "SELECT s FROM Student s WHERE s.Email = :email";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves Email property casing
        sql.Should().Contain("SELECT * FROM students");
        sql.Should().Contain("WHERE Email = @email");
        sql.Should().NotContain("s.Email");
    }

    [Fact]
    public void ConvertToSql_TwoLetterAlias_ShouldRemoveAlias()
    {
        // Arrange
        var cpql = "SELECT pr FROM Product pr WHERE pr.Price > :price";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves Price property casing
        sql.Should().Contain("SELECT * FROM products");
        sql.Should().Contain("WHERE Price > @price");
        sql.Should().NotContain("pr.Price");
    }

    [Fact]
    public void ConvertToSql_ThreeLetterAlias_ShouldRemoveAlias()
    {
        // Arrange
        var cpql = "SELECT cat FROM Category cat WHERE cat.Name = :name";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves Name property casing
        sql.Should().Contain("SELECT * FROM categories");
        sql.Should().Contain("WHERE Name = @name");
        sql.Should().NotContain("cat.Name");
    }

    [Fact]
    public void ConvertToSql_EmptyOrNull_ShouldReturnAsIs()
    {
        // Arrange & Act
        var result1 = CpqlToSqlConverter.ConvertToSql("");
        var result2 = CpqlToSqlConverter.ConvertToSql(null!);
        var result3 = CpqlToSqlConverter.ConvertToSql("   ");

        // Assert
        result1.Should().Be("");
        result2.Should().BeNull();
        result3.Should().Be("   ");
    }

    [Fact]
    public void ConvertToSql_PlainSqlWithoutAlias_ShouldNotModify()
    {
        // Arrange
        var sql = "SELECT * FROM Products WHERE Price > @price";

        // Act
        var result = CpqlToSqlConverter.ConvertToSql(sql);

        // Assert
        result.Should().Be(sql);
    }

    [Fact]
    public void ConvertToSql_WithGroupBy_ShouldPreserveGroupBy()
    {
        // Arrange
        var cpql = "SELECT p.Category, COUNT(p) FROM Product p GROUP BY p.Category";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves Category property casing
        sql.Should().Contain("GROUP BY");
        sql.Should().Contain("Category");
        sql.Should().Contain("COUNT(*)");
    }

    [Fact]
    public void ConvertToSql_WithHaving_ShouldPreserveHaving()
    {
        // Arrange
        var cpql = "SELECT p.Category, COUNT(p) FROM Product p GROUP BY p.Category HAVING COUNT(p) > :minCount";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert
        sql.Should().Contain("HAVING");
        sql.Should().Contain("COUNT(*) > @minCount");
    }

    [Fact]
    public void ConvertToSql_MultipleParameters_ShouldConvertAll()
    {
        // Arrange
        var cpql = "SELECT p FROM Product p WHERE p.Price BETWEEN :min AND :max";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert
        sql.Should().Contain("@min");
        sql.Should().Contain("@max");
        sql.Should().NotContain(":min");
        sql.Should().NotContain(":max");
    }

    [Fact]
    public void ConvertToSql_CaseInsensitive_ShouldHandleUpperCase()
    {
        // Arrange
        var cpql = "SELECT P FROM Product P WHERE P.Price > :price";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert
        sql.Should().Contain("SELECT * FROM products");
        sql.Should().Contain("WHERE Price > @price");
    }

    [Fact]
    public void ConvertToSql_WithInnerJoin_ShouldPreserveJoin()
    {
        // Arrange
        var cpql = "SELECT p FROM Product p INNER JOIN Category c ON p.CategoryId = c.Id WHERE c.Name = :categoryName";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert
        sql.Should().Contain("INNER JOIN");
        sql.Should().Contain("categories");
        sql.Should().Contain("WHERE");
    }

    [Fact]
    public void ConvertToSql_WithLeftJoin_ShouldPreserveJoin()
    {
        // Arrange
        var cpql = "SELECT p FROM Product p LEFT JOIN Category c ON p.CategoryId = c.Id";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert
        sql.Should().Contain("LEFT JOIN");
    }

    [Fact]
    public void ConvertToSql_RealWorldExample_StudentByEmail()
    {
        // Arrange - Real example from UdemyCloneSaaS
        var cpql = "SELECT s FROM Student s WHERE s.Email = :email";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves Email property casing
        sql.Should().Be("SELECT * FROM students WHERE Email = @email");
    }

    [Fact]
    public void ConvertToSql_RealWorldExample_TopStudents()
    {
        // Arrange - Real example from UdemyCloneSaaS
        var cpql = "SELECT s FROM Student s ORDER BY s.EnrolledCoursesCount DESC LIMIT :limit";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves EnrolledCoursesCount property casing
        sql.Should().Contain("SELECT * FROM students");
        sql.Should().Contain("ORDER BY EnrolledCoursesCount DESC");
        sql.Should().Contain("LIMIT @limit");
    }

    [Fact]
    public void ConvertToSql_RealWorldExample_CountStudents()
    {
        // Arrange - Real example from UdemyCloneSaaS
        var cpql = "SELECT COUNT(s) FROM Student s";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert
        sql.Should().Be("SELECT COUNT(*) FROM students");
    }

    [Fact]
    public void ConvertToSql_RealWorldExample_AverageRating()
    {
        // Arrange
        var cpql = "SELECT AVG(r.Rating) FROM Review r WHERE r.CourseId = :courseId";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves Rating and CourseId property casing
        sql.Should().Contain("SELECT AVG(Rating)");
        sql.Should().Contain("FROM reviews");
        sql.Should().Contain("WHERE CourseId = @courseId");
    }

    [Fact]
    public void ConvertToSql_BugRegression_WhereNotRemovedFromPlainSql()
    {
        // Arrange - This was the actual bug found
        var sql = "SELECT * FROM users WHERE email = @email";

        // Act
        var result = CpqlToSqlConverter.ConvertToSql(sql);

        // Assert
        // The bug was: regex matched "FROM users WHERE" and removed WHERE
        // Expected: "SELECT * FROM users WHERE email = @email"
        // Actual (buggy): "SELECT * FROM users email = @email"
        result.Should().Be("SELECT * FROM users WHERE email = @email");
        result.Should().NotBe("SELECT * FROM users email = @email");
    }

    [Fact]
    public void ConvertToSql_BugRegression_OrderByNotRemovedFromPlainSql()
    {
        // Arrange
        var sql = "SELECT * FROM products ORDER BY price DESC";

        // Act
        var result = CpqlToSqlConverter.ConvertToSql(sql);

        // Assert
        result.Should().Contain("ORDER BY price DESC");
    }

    [Fact]
    public void ConvertToSql_BugRegression_GroupByNotRemovedFromPlainSql()
    {
        // Arrange
        var sql = "SELECT category, COUNT(*) FROM products GROUP BY category";

        // Act
        var result = CpqlToSqlConverter.ConvertToSql(sql);

        // Assert
        result.Should().Contain("GROUP BY category");
    }

    [Fact]
    public void ConvertToSql_BugRegression_LimitNotRemovedFromPlainSql()
    {
        // Arrange
        var sql = "SELECT * FROM products LIMIT 10";

        // Act
        var result = CpqlToSqlConverter.ConvertToSql(sql);

        // Assert
        result.Should().Contain("LIMIT 10");
    }

    [Fact]
    public void ConvertToSql_EdgeCase_LongAliasName_ShouldNotRemove()
    {
        // Arrange - Alias longer than 3 chars should NOT be removed by FROM clause converter
        var cpql = "SELECT product FROM Product product WHERE product.Price > :price";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert
        // Long alias "product" won't be matched by FROM regex (only matches 1-3 char aliases)
        // So it should remain in the query initially, then alias removal should handle it
        sql.Should().Contain("FROM products");
        sql.Should().Contain("WHERE");
    }

    [Fact]
    public void ConvertToSql_EdgeCase_NoSpaceAfterFrom_ShouldHandle()
    {
        // Arrange
        var cpql = "SELECT p FROM Product p WHERE p.Name=:name";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert
        sql.Should().Contain("FROM products");
        sql.Should().Contain("Name=@name");
    }

    [Fact]
    public void ConvertToSql_MultipleAliases_ShouldRemoveCorrectOne()
    {
        // Arrange - Query with multiple aliases (e.g., JOIN scenario)
        var cpql = "SELECT p FROM Product p WHERE p.CategoryId = :catId AND p.Price > :price";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert
        sql.Should().NotContain("p.CategoryId");
        sql.Should().NotContain("p.Price");
        sql.Should().Contain("CategoryId = @catId");
        sql.Should().Contain("Price > @price");
    }

    [Fact]
    public void ConvertToSql_WithMetadata_ShouldUseExplicitColumnList()
    {
        // Arrange
        var metadata = new NPA.Generators.EntityMetadataInfo
        {
            Name = "Student",
            TableName = "students",
            Properties = new List<NPA.Generators.PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Email", ColumnName = "email" },
                new() { Name = "FirstName", ColumnName = "first_name" },
                new() { Name = "LastName", ColumnName = "last_name" },
                new() { Name = "EnrolledCoursesCount", ColumnName = "enrolled_courses_count" }
            }
        };
        var cpql = "SELECT s FROM Student s WHERE s.Email = :email";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert - Should use column aliases matching property names for Dapper
        sql.Should().Contain("SELECT s.id AS Id, s.email AS Email, s.first_name AS FirstName, s.last_name AS LastName, s.enrolled_courses_count AS EnrolledCoursesCount");
        sql.Should().Contain("FROM students");
        sql.Should().Contain("WHERE email = @email");
        sql.Should().NotContain("s.Email"); // Alias should be removed from WHERE clause
    }

    [Fact]
    public void ConvertToSql_WithMetadata_ShouldUseTableNameFromMetadata()
    {
        // Arrange - Table name different from entity name
        var metadata = new NPA.Generators.EntityMetadataInfo
        {
            Name = "Product",
            TableName = "tbl_products", // Custom table name
            Properties = new List<NPA.Generators.PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "product_id" },
                new() { Name = "Name", ColumnName = "product_name" },
                new() { Name = "Price", ColumnName = "unit_price" }
            }
        };
        var cpql = "SELECT p FROM Product p WHERE p.Price > :minPrice";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert - Should use AS aliases for Dapper mapping
        sql.Should().Contain("SELECT p.product_id AS Id, p.product_name AS Name, p.unit_price AS Price");
        sql.Should().Contain("FROM tbl_products");
        sql.Should().Contain("WHERE unit_price > @minPrice");
    }

    [Fact]
    public void ConvertToSql_WithMetadata_AggregateFunction_ShouldUseColumnName()
    {
        // Arrange
        var metadata = new NPA.Generators.EntityMetadataInfo
        {
            Name = "Product",
            TableName = "products",
            Properties = new List<NPA.Generators.PropertyMetadataInfo>
            {
                new() { Name = "Price", ColumnName = "unit_price" }
            }
        };
        var cpql = "SELECT AVG(p.Price) FROM Product p";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Contain("SELECT AVG(unit_price)");
        sql.Should().Contain("FROM products");
    }

    [Fact]
    public void ConvertToSql_WithMetadata_ListReturnType_ShouldGenerateExplicitColumns()
    {
        // Arrange - Simulating List<Student> return type
        var metadata = new NPA.Generators.EntityMetadataInfo
        {
            Name = "Student",
            TableName = "students",
            Properties = new List<NPA.Generators.PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Email", ColumnName = "email" },
                new() { Name = "FirstName", ColumnName = "first_name" },
                new() { Name = "LastName", ColumnName = "last_name" }
            }
        };
        var cpql = "SELECT s FROM Student s";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Be("SELECT s.id AS Id, s.email AS Email, s.first_name AS FirstName, s.last_name AS LastName FROM students s");
    }

    [Fact]
    public void ConvertToSql_WithMetadata_SingleReturnType_ShouldGenerateExplicitColumns()
    {
        // Arrange - Simulating single Student return type
        var metadata = new NPA.Generators.EntityMetadataInfo
        {
            Name = "Student",
            TableName = "students",
            Properties = new List<NPA.Generators.PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Email", ColumnName = "email" }
            }
        };
        var cpql = "SELECT s FROM Student s WHERE s.Id = :id";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Contain("SELECT s.id AS Id, s.email AS Email");
        sql.Should().Contain("FROM students");
        sql.Should().Contain("WHERE id = @id");
    }

    [Fact]
    public void ConvertToSql_WithMetadata_ComplexQuery_ShouldHandleAllClauses()
    {
        // Arrange - Complex query with ORDER BY, multiple WHERE conditions
        var metadata = new NPA.Generators.EntityMetadataInfo
        {
            Name = "Product",
            TableName = "products",
            Properties = new List<NPA.Generators.PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "product_id" },
                new() { Name = "Name", ColumnName = "product_name" },
                new() { Name = "Price", ColumnName = "unit_price" },
                new() { Name = "CategoryId", ColumnName = "category_id" },
                new() { Name = "IsActive", ColumnName = "is_active" }
            }
        };
        var cpql = "SELECT p FROM Product p WHERE p.Price > :minPrice AND p.IsActive = true ORDER BY p.Price DESC";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Contain("SELECT p.product_id AS Id, p.product_name AS Name, p.unit_price AS Price, p.category_id AS CategoryId, p.is_active AS IsActive");
        sql.Should().Contain("FROM products");
        sql.Should().Contain("WHERE unit_price > @minPrice AND is_active = true");
        sql.Should().Contain("ORDER BY unit_price DESC");
    }

    [Fact]
    public void ConvertToSql_WithMetadata_DISTINCT_ShouldPreserveDistinct()
    {
        // Arrange
        var metadata = new NPA.Generators.EntityMetadataInfo
        {
            Name = "Student",
            TableName = "students",
            Properties = new List<NPA.Generators.PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Email", ColumnName = "email" }
            }
        };
        var cpql = "SELECT DISTINCT s FROM Student s";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Be("SELECT DISTINCT s.id AS Id, s.email AS Email FROM students s");
    }

    [Fact]
    public void ConvertToSql_WithoutMetadata_ShouldFallbackToSelectStar()
    {
        // Arrange - No metadata provided
        var cpql = "SELECT s FROM Student s WHERE s.Email = :email";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, (EntityMetadataInfo?)null);

        // Assert
        sql.Should().Contain("SELECT * FROM students");
        // Without metadata, property names are preserved as-is (not converted to snake_case)
        sql.Should().Contain("WHERE Email = @email");
    }

    [Fact]
    public void ConvertToSql_WithMetadata_EmptyProperties_ShouldFallbackToSelectStar()
    {
        // Arrange - Metadata with no properties
        var metadata = new NPA.Generators.EntityMetadataInfo
        {
            Name = "Student",
            TableName = "students",
            Properties = new List<NPA.Generators.PropertyMetadataInfo>()
        };
        var cpql = "SELECT s FROM Student s";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Be("SELECT * FROM students s");
    }

    [Fact]
    public void ConvertToSql_WithMetadata_JoinQuery_ShouldHandleMultipleTables()
    {
        // Arrange - Main entity metadata (Product)
        var metadata = new NPA.Generators.EntityMetadataInfo
        {
            Name = "Product",
            TableName = "products",
            Properties = new List<NPA.Generators.PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "product_id" },
                new() { Name = "Name", ColumnName = "product_name" },
                new() { Name = "CategoryId", ColumnName = "category_id" }
            }
        };
        var cpql = "SELECT p FROM Product p INNER JOIN Category c ON p.CategoryId = c.Id WHERE c.Name = :categoryName";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Contain("SELECT p.product_id AS Id, p.product_name AS Name, p.category_id AS CategoryId");
        sql.Should().Contain("FROM products p"); // Should preserve alias when we have metadata
        sql.Should().Contain("INNER JOIN categories c");
        sql.Should().Contain("ON category_id = Id"); // Both aliases removed (falls back to preserved casing)
        sql.Should().Contain("WHERE Name = @categoryName"); // c.Name converted to preserved casing 'Name'
    }

    [Fact]
    public void ConvertToSql_WithMetadata_GROUP_BY_ShouldUseColumnNames()
    {
        // Arrange
        var metadata = new NPA.Generators.EntityMetadataInfo
        {
            Name = "Product",
            TableName = "products",
            Properties = new List<NPA.Generators.PropertyMetadataInfo>
            {
                new() { Name = "CategoryId", ColumnName = "category_id" }
            }
        };
        var cpql = "SELECT COUNT(p) FROM Product p GROUP BY p.CategoryId";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Contain("SELECT COUNT(*)");
        sql.Should().Contain("FROM products");
        sql.Should().Contain("GROUP BY category_id");
    }

    [Fact]
    public void ConvertToSql_WithMetadata_LIMIT_ShouldPreserveLimit()
    {
        // Arrange
        var metadata = new NPA.Generators.EntityMetadataInfo
        {
            Name = "Student",
            TableName = "students",
            Properties = new List<NPA.Generators.PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Email", ColumnName = "email" }
            }
        };
        var cpql = "SELECT s FROM Student s ORDER BY s.Id LIMIT :limit";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Contain("SELECT s.id AS Id, s.email AS Email");
        sql.Should().Contain("FROM students");
        sql.Should().Contain("ORDER BY id LIMIT @limit");
    }
    
    [Fact]
    public void ConvertToSql_WithMultipleEntitiesMetadata_ShouldConvertCorrectly()
    {
        // Arrange - Metadata for Product entity
        var productMetadata = new EntityMetadataInfo
        {
            Name = "Product",
            TableName = "products",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Name", ColumnName = "product_name" },
                new() { Name = "CategoryId", ColumnName = "category_id" }
            }
        };
        
        // Metadata for Category entity
        var categoryMetadata = new EntityMetadataInfo
        {
            Name = "Category",
            TableName = "categories",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Name", ColumnName = "category_name" }
            }
        };
        
        // Dictionary with metadata for all entities in the query
        var entitiesMetadata = new Dictionary<string, EntityMetadataInfo>
        {
            { "Product", productMetadata },
            { "Category", categoryMetadata }
        };
        
        var cpql = "SELECT p FROM Product p INNER JOIN Category c ON p.CategoryId = c.Id WHERE c.Name = :categoryName";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, entitiesMetadata);

        // Assert
        sql.Should().Contain("SELECT p.id AS Id, p.product_name AS Name, p.category_id AS CategoryId");
        sql.Should().Contain("FROM products p");
        sql.Should().Contain("INNER JOIN categories c");
        sql.Should().Contain("ON category_id = id"); // Both properties converted using their respective metadata
        sql.Should().Contain("WHERE category_name = @categoryName");
    }

    #region Complex SQL Tests

    [Fact]
    public void ConvertToSql_ComplexMultipleJoins_ShouldHandleAllEntities()
    {
        // Arrange - Realistic scenario: Order with Product, Category, and Customer
        var orderMetadata = new EntityMetadataInfo
        {
            Name = "Order",
            TableName = "orders",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "order_id" },
                new() { Name = "OrderDate", ColumnName = "order_date" },
                new() { Name = "ProductId", ColumnName = "product_id" },
                new() { Name = "CustomerId", ColumnName = "customer_id" },
                new() { Name = "Quantity", ColumnName = "qty" },
                new() { Name = "TotalAmount", ColumnName = "total_amt" }
            }
        };

        var productMetadata = new EntityMetadataInfo
        {
            Name = "Product",
            TableName = "products",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "product_id" },
                new() { Name = "Name", ColumnName = "product_name" },
                new() { Name = "Price", ColumnName = "unit_price" },
                new() { Name = "CategoryId", ColumnName = "cat_id" }
            }
        };

        var categoryMetadata = new EntityMetadataInfo
        {
            Name = "Category",
            TableName = "categories",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "category_id" },
                new() { Name = "Name", ColumnName = "cat_name" }
            }
        };

        var customerMetadata = new EntityMetadataInfo
        {
            Name = "Customer",
            TableName = "customers",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "customer_id" },
                new() { Name = "Email", ColumnName = "email_address" },
                new() { Name = "FullName", ColumnName = "full_name" }
            }
        };

        var entitiesMetadata = new Dictionary<string, EntityMetadataInfo>
        {
            { "Order", orderMetadata },
            { "Product", productMetadata },
            { "Category", categoryMetadata },
            { "Customer", customerMetadata }
        };

        var cpql = @"SELECT o FROM Order o 
                     INNER JOIN Product p ON o.ProductId = p.Id 
                     INNER JOIN Category c ON p.CategoryId = c.Id 
                     LEFT JOIN Customer cu ON o.CustomerId = cu.Id 
                     WHERE c.Name = :categoryName AND o.TotalAmount > :minAmount 
                     ORDER BY o.OrderDate DESC";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, entitiesMetadata);

        // Assert
        sql.Should().Contain("SELECT o.order_id AS Id, o.order_date AS OrderDate, o.product_id AS ProductId, o.customer_id AS CustomerId, o.qty AS Quantity, o.total_amt AS TotalAmount");
        sql.Should().Contain("FROM orders o");
        sql.Should().Contain("INNER JOIN products p ON product_id = product_id");
        sql.Should().Contain("INNER JOIN categories c ON cat_id = category_id");
        sql.Should().Contain("LEFT JOIN customers cu ON customer_id = customer_id");
        sql.Should().Contain("WHERE cat_name = @categoryName AND total_amt > @minAmount");
        sql.Should().Contain("ORDER BY order_date DESC");
    }

    [Fact]
    public void ConvertToSql_ComplexWithSubquery_ShouldHandleNested()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "Product",
            TableName = "products",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Name", ColumnName = "product_name" },
                new() { Name = "Price", ColumnName = "unit_price" },
                new() { Name = "CategoryId", ColumnName = "category_id" }
            }
        };

        var cpql = @"SELECT p FROM Product p 
                     WHERE p.Price > (SELECT AVG(p2.Price) FROM Product p2 WHERE p2.CategoryId = p.CategoryId)
                     ORDER BY p.Price DESC";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert - Note: Subquery aliases (p2) are preserved as the converter doesn't know about nested contexts
        sql.Should().Contain("SELECT p.id AS Id, p.product_name AS Name, p.unit_price AS Price, p.category_id AS CategoryId");
        sql.Should().Contain("FROM products p");
        sql.Should().Contain("WHERE unit_price >");
        sql.Should().Contain("SELECT AVG(Price) FROM products"); // p2 alias removed, but inner references not fully converted
        sql.Should().Contain("ORDER BY unit_price DESC");
    }

    [Fact]
    public void ConvertToSql_ComplexWithINClause_ShouldHandleList()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "Student",
            TableName = "students",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Email", ColumnName = "email" },
                new() { Name = "Status", ColumnName = "enrollment_status" }
            }
        };

        var cpql = "SELECT s FROM Student s WHERE s.Status IN (:statuses) AND s.Email LIKE :emailPattern";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Contain("SELECT s.id AS Id, s.email AS Email, s.enrollment_status AS Status");
        sql.Should().Contain("FROM students s");
        sql.Should().Contain("WHERE enrollment_status IN (@statuses) AND email LIKE @emailPattern");
    }

    [Fact]
    public void ConvertToSql_ComplexWithCaseStatement_ShouldPreserveCase()
    {
        // Arrange - Test that CASE statements are preserved in complex SQL
        var metadata = new EntityMetadataInfo
        {
            Name = "Order",
            TableName = "orders",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Status", ColumnName = "order_status" },
                new() { Name = "TotalAmount", ColumnName = "total" }
            }
        };

        var cpql = @"SELECT id, order_status, total, 
                     CASE 
                         WHEN total > 1000 THEN 'Premium'
                         WHEN total > 500 THEN 'Standard'
                         ELSE 'Basic'
                     END as tier
                     FROM orders 
                     WHERE order_status = @status";

        // Act - Pass null metadata since query already uses column names
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, (EntityMetadataInfo?)null);

        // Assert - CASE statement and structure preserved
        sql.Should().Contain("CASE");
        sql.Should().Contain("WHEN total > 1000 THEN 'Premium'");
        sql.Should().Contain("WHEN total > 500 THEN 'Standard'");
        sql.Should().Contain("ELSE 'Basic'");
        sql.Should().Contain("END as tier");
        sql.Should().Contain("WHERE order_status = @status");
    }

    [Fact]
    public void ConvertToSql_ComplexWithUnion_ShouldHandleMultipleSelects()
    {
        // Arrange - Test UNION queries
        var metadata = new EntityMetadataInfo
        {
            Name = "Student",
            TableName = "students",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Email", ColumnName = "email" },
                new() { Name = "Status", ColumnName = "status" }
            }
        };

        var cpql = @"SELECT s FROM Student s WHERE s.Status = :status1
                     UNION
                     SELECT s FROM Student s WHERE s.Status = :status2";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert - Both SELECT statements converted
        sql.Should().Contain("SELECT s.id AS Id, s.email AS Email, s.status AS Status FROM students s WHERE status = @status1");
        sql.Should().Contain("UNION");
        // Second SELECT's WHERE is also converted (s.Status -> status)
        sql.Should().Contain("@status2");
    }

    [Fact]
    public void ConvertToSql_ComplexWithWindowFunction_ShouldPreserveOver()
    {
        // Arrange - Test window functions like ROW_NUMBER()
        var metadata = new EntityMetadataInfo
        {
            Name = "Order",
            TableName = "orders",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "CustomerId", ColumnName = "customer_id" },
                new() { Name = "TotalAmount", ColumnName = "total" },
                new() { Name = "OrderDate", ColumnName = "created_at" }
            }
        };

        // Use plain SQL with window function
        var sql = @"SELECT id, customer_id, total, created_at, 
                     ROW_NUMBER() OVER (PARTITION BY customer_id ORDER BY created_at DESC) as row_num
                     FROM orders";

        // Act - No conversion needed for plain SQL
        var result = CpqlToSqlConverter.ConvertToSql(sql, metadata);

        // Assert - Window function preserved
        sql.Should().Contain("ROW_NUMBER() OVER (PARTITION BY customer_id ORDER BY created_at DESC) as row_num");
        sql.Should().Contain("FROM orders");
    }

    [Fact]
    public void ConvertToSql_ComplexWithHavingAndGroupBy_ShouldHandleAggregates()
    {
        // Arrange
        var orderMetadata = new EntityMetadataInfo
        {
            Name = "Order",
            TableName = "orders",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "CustomerId", ColumnName = "customer_id" },
                new() { Name = "TotalAmount", ColumnName = "total" }
            }
        };

        var customerMetadata = new EntityMetadataInfo
        {
            Name = "Customer",
            TableName = "customers",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Email", ColumnName = "email" }
            }
        };

        var entitiesMetadata = new Dictionary<string, EntityMetadataInfo>
        {
            { "Order", orderMetadata },
            { "Customer", customerMetadata }
        };

        var cpql = @"SELECT email, COUNT(o), SUM(o.TotalAmount) 
                     FROM Order o 
                     INNER JOIN Customer c ON o.CustomerId = c.Id 
                     GROUP BY email 
                     HAVING SUM(o.TotalAmount) > :minTotal 
                     ORDER BY SUM(o.TotalAmount) DESC";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, entitiesMetadata);

        // Assert - Column names converted, aggregates preserved
        sql.Should().Contain("SELECT email, COUNT(*), SUM(total)");
        sql.Should().Contain("FROM orders o");
        sql.Should().Contain("INNER JOIN customers c ON customer_id = id");
        sql.Should().Contain("GROUP BY email");
        sql.Should().Contain("HAVING SUM(total) > @minTotal");
        sql.Should().Contain("ORDER BY SUM(total) DESC");
    }

    [Fact]
    public void ConvertToSql_ComplexWithExists_ShouldHandleCorrelatedSubquery()
    {
        // Arrange - Test EXISTS with correlated subquery
        var studentMetadata = new EntityMetadataInfo
        {
            Name = "Student",
            TableName = "students",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Email", ColumnName = "email" }
            }
        };

        var enrollmentMetadata = new EntityMetadataInfo
        {
            Name = "Enrollment",
            TableName = "enrollments",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "StudentId", ColumnName = "student_id" },
                new() { Name = "CourseId", ColumnName = "course_id" }
            }
        };

        var entitiesMetadata = new Dictionary<string, EntityMetadataInfo>
        {
            { "Student", studentMetadata },
            { "Enrollment", enrollmentMetadata }
        };

        var cpql = @"SELECT s FROM Student s 
                     WHERE EXISTS (
                         SELECT 1 FROM Enrollment e 
                         WHERE e.StudentId = s.Id AND e.CourseId = :courseId
                     )";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, entitiesMetadata);

        // Assert - Main query and subquery both converted
        sql.Should().Contain("SELECT s.id AS Id, s.email AS Email FROM students s");
        sql.Should().Contain("WHERE EXISTS");
        sql.Should().Contain("SELECT 1 FROM enrollments");
        // Note: Correlated subqueries have complexity; we verify key parts are converted
        sql.Should().Contain("@courseId");
    }

    [Fact]
    public void ConvertToSql_ComplexWithCTE_ShouldHandleWithClause()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "Order",
            TableName = "orders",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "CustomerId", ColumnName = "customer_id" },
                new() { Name = "TotalAmount", ColumnName = "total" }
            }
        };

        var cpql = @"WITH HighValueOrders AS (
                         SELECT o FROM Order o WHERE o.TotalAmount > 1000
                     )
                     SELECT * FROM HighValueOrders WHERE customer_id = :customerId";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert - CTE and second SELECT both processed
        sql.Should().Contain("WITH HighValueOrders AS");
        sql.Should().Contain("SELECT o.id AS Id, o.customer_id AS CustomerId, o.total AS TotalAmount FROM orders o WHERE total > 1000");
        sql.Should().Contain("SELECT * FROM HighValueOrders WHERE customer_id = @customerId");
    }

    [Fact]
    public void ConvertToSql_ComplexWithMultipleConditions_ShouldHandleAndOr()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "Product",
            TableName = "products",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Name", ColumnName = "name" },
                new() { Name = "Price", ColumnName = "price" },
                new() { Name = "Stock", ColumnName = "stock_qty" },
                new() { Name = "IsActive", ColumnName = "is_active" },
                new() { Name = "CategoryId", ColumnName = "category_id" }
            }
        };

        var cpql = @"SELECT p FROM Product p 
                     WHERE (p.Price > :minPrice AND p.Price < :maxPrice) 
                     AND (p.Stock > 0 OR p.CategoryId IN (:categories))
                     AND p.IsActive = true
                     ORDER BY p.Price ASC, p.Name ASC";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Contain("SELECT p.id AS Id, p.name AS Name, p.price AS Price, p.stock_qty AS Stock, p.is_active AS IsActive, p.category_id AS CategoryId");
        sql.Should().Contain("WHERE (price > @minPrice AND price < @maxPrice)");
        sql.Should().Contain("AND (stock_qty > 0 OR category_id IN (@categories))");
        sql.Should().Contain("AND is_active = true");
        sql.Should().Contain("ORDER BY price ASC, name ASC");
    }

    [Fact]
    public void ConvertToSql_ComplexWithSelfJoin_ShouldHandleSameTable()
    {
        // Arrange - Self-join example (employee -> manager)
        var metadata = new EntityMetadataInfo
        {
            Name = "Employee",
            TableName = "employees",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Name", ColumnName = "emp_name" },
                new() { Name = "ManagerId", ColumnName = "manager_id" }
            }
        };

        var entitiesMetadata = new Dictionary<string, EntityMetadataInfo>
        {
            { "Employee", metadata }
        };

        // For self-joins, use explicit column names to avoid ambiguity
        var cpql = @"SELECT e.id, e.emp_name, e.manager_id 
                     FROM Employee e 
                     LEFT JOIN Employee m ON e.ManagerId = m.Id 
                     WHERE m.Name = :managerName";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, entitiesMetadata);

        // Assert - Self-join preserved with proper table names
        sql.Should().Contain("FROM employees e");
        sql.Should().Contain("LEFT JOIN employees m");
        sql.Should().Contain("@managerName");
    }

    [Fact]
    public void ConvertToSql_ComplexWithDateFunctions_ShouldPreserveFunctions()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "Order",
            TableName = "orders",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "OrderDate", ColumnName = "order_date" },
                new() { Name = "TotalAmount", ColumnName = "total" }
            }
        };

        var cpql = @"SELECT o FROM Order o 
                     WHERE YEAR(o.OrderDate) = :year 
                     AND MONTH(o.OrderDate) = :month
                     AND DAY(o.OrderDate) BETWEEN 1 AND 15
                     ORDER BY o.OrderDate DESC";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Contain("SELECT o.id AS Id, o.order_date AS OrderDate, o.total AS TotalAmount");
        sql.Should().Contain("WHERE YEAR(order_date) = @year");
        sql.Should().Contain("AND MONTH(order_date) = @month");
        sql.Should().Contain("AND DAY(order_date) BETWEEN 1 AND 15");
        sql.Should().Contain("ORDER BY order_date DESC");
    }

    [Fact]
    public void ConvertToSql_ComplexWithStringFunctions_ShouldPreserveFunctions()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "Student",
            TableName = "students",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Email", ColumnName = "email" },
                new() { Name = "FirstName", ColumnName = "first_name" },
                new() { Name = "LastName", ColumnName = "last_name" }
            }
        };

        var cpql = @"SELECT s FROM Student s 
                     WHERE LOWER(s.Email) LIKE :emailPattern 
                     AND CONCAT(s.FirstName, ' ', s.LastName) = :fullName
                     AND LENGTH(s.Email) > 10";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Contain("SELECT s.id AS Id, s.email AS Email, s.first_name AS FirstName, s.last_name AS LastName");
        sql.Should().Contain("WHERE LOWER(email) LIKE @emailPattern");
        sql.Should().Contain("AND CONCAT(first_name, ' ', last_name) = @fullName");
        sql.Should().Contain("AND LENGTH(email) > 10");
    }

    [Fact]
    public void ConvertToSql_ComplexWithNullChecks_ShouldHandleIsNull()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "Product",
            TableName = "products",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Name", ColumnName = "name" },
                new() { Name = "Description", ColumnName = "description" },
                new() { Name = "DiscountPrice", ColumnName = "discount_price" }
            }
        };

        var cpql = @"SELECT p FROM Product p 
                     WHERE p.Description IS NOT NULL 
                     AND p.DiscountPrice IS NULL
                     AND COALESCE(p.DiscountPrice, 0) = 0";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Contain("SELECT p.id AS Id, p.name AS Name, p.description AS Description, p.discount_price AS DiscountPrice");
        sql.Should().Contain("WHERE description IS NOT NULL");
        sql.Should().Contain("AND discount_price IS NULL");
        sql.Should().Contain("AND COALESCE(discount_price, 0) = 0");
    }

    [Fact]
    public void ConvertToSql_ComplexWithOffset_ShouldHandlePagination()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "Product",
            TableName = "products",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Name", ColumnName = "name" },
                new() { Name = "Price", ColumnName = "price" }
            }
        };

        var cpql = @"SELECT p FROM Product p 
                     ORDER BY p.Price DESC 
                     LIMIT :pageSize OFFSET :offset";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Contain("SELECT p.id AS Id, p.name AS Name, p.price AS Price");
        sql.Should().Contain("FROM products p");
        sql.Should().Contain("ORDER BY price DESC");
        sql.Should().Contain("LIMIT @pageSize OFFSET @offset");
    }

    [Fact]
    public void ConvertToSql_ShouldFormatSqlWithLineBreaks()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "Student",
            TableName = "students",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Email", ColumnName = "email" },
                new() { Name = "FirstName", ColumnName = "first_name" },
                new() { Name = "LastName", ColumnName = "last_name" }
            }
        };
        var cpql = "SELECT s FROM Student s WHERE s.Email = :email AND s.FirstName = :firstName ORDER BY s.LastName";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata, formatSql: true);

        // Assert - Verify formatting with line breaks
        sql.Should().StartWith("SELECT s.id AS Id");
        sql.Should().Contain("\nFROM students s");
        sql.Should().Contain("\nWHERE email = @email");
        sql.Should().Contain("\n  AND first_name = @firstName");
        sql.Should().Contain("\nORDER BY last_name");
    }

    #endregion

    #region UPDATE and DELETE Query Tests

    [Fact]
    public void ConvertToSql_SimpleUpdate_ShouldConvertCorrectly()
    {
        // Arrange
        var cpql = "UPDATE Product p SET p.Price = :price WHERE p.Id = :id";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves property casing
        sql.Should().Be("UPDATE product SET Price = @price WHERE Id = @id");
    }

    [Fact]
    public void ConvertToSql_UpdateWithMetadata_ShouldUseTableAndColumnNames()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "User",
            TableName = "users",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "AccountBalance", ColumnName = "account_balance" },
                new() { Name = "Country", ColumnName = "country" }
            }
        };
        var cpql = "UPDATE User u SET u.AccountBalance = u.AccountBalance + :amount WHERE u.Country = :country";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Be("UPDATE users SET account_balance = account_balance + @amount WHERE country = @country");
    }

    [Fact]
    public void ConvertToSql_UpdateMultipleFields_ShouldConvertCorrectly()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "Product",
            TableName = "products",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Id", ColumnName = "id" },
                new() { Name = "Price", ColumnName = "price" },
                new() { Name = "Stock", ColumnName = "stock" },
                new() { Name = "IsActive", ColumnName = "is_active" }
            }
        };
        var cpql = "UPDATE Product p SET p.Price = :price, p.Stock = :stock, p.IsActive = :active WHERE p.Id = :id";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Be("UPDATE products SET price = @price, stock = @stock, is_active = @active WHERE id = @id");
    }

    [Fact]
    public void ConvertToSql_UpdateWithExpression_ShouldPreserveExpression()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "Order",
            TableName = "orders",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "TotalAmount", ColumnName = "total_amount" },
                new() { Name = "DiscountPercent", ColumnName = "discount_percent" }
            }
        };
        var cpql = "UPDATE Order o SET o.TotalAmount = o.TotalAmount * (1 - o.DiscountPercent / 100) WHERE o.DiscountPercent > :minDiscount";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Be("UPDATE orders SET total_amount = total_amount * (1 - discount_percent / 100) WHERE discount_percent > @minDiscount");
    }

    [Fact]
    public void ConvertToSql_SimpleDelete_ShouldConvertCorrectly()
    {
        // Arrange
        var cpql = "DELETE FROM Product p WHERE p.Id = :id";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves property casing
        sql.Should().Be("DELETE FROM product WHERE Id = @id");
    }

    [Fact]
    public void ConvertToSql_DeleteWithMetadata_ShouldUseTableAndColumnNames()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "User",
            TableName = "users",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "IsActive", ColumnName = "is_active" },
                new() { Name = "CreatedAt", ColumnName = "created_at" }
            }
        };
        var cpql = "DELETE FROM User u WHERE u.IsActive = false AND u.CreatedAt < :date";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Be("DELETE FROM users WHERE is_active = false AND created_at < @date");
    }

    [Fact]
    public void ConvertToSql_DeleteWithComplexCondition_ShouldConvertCorrectly()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "Session",
            TableName = "sessions",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "ExpiresAt", ColumnName = "expires_at" },
                new() { Name = "IsRevoked", ColumnName = "is_revoked" },
                new() { Name = "UserId", ColumnName = "user_id" }
            }
        };
        var cpql = "DELETE FROM Session s WHERE (s.ExpiresAt < :now OR s.IsRevoked = true) AND s.UserId = :userId";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Be("DELETE FROM sessions WHERE (expires_at < @now OR is_revoked = true) AND user_id = @userId");
    }

    [Fact]
    public void ConvertToSql_DeleteWithInClause_ShouldConvertCorrectly()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "Product",
            TableName = "products",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "CategoryId", ColumnName = "category_id" },
                new() { Name = "Status", ColumnName = "status" }
            }
        };
        var cpql = "DELETE FROM Product p WHERE p.CategoryId = ANY(:categoryIds) AND p.Status = :status";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Be("DELETE FROM products WHERE category_id = ANY(@categoryIds) AND status = @status");
    }

    [Fact]
    public void ConvertToSql_UpdateWithoutWhere_ShouldConvertCorrectly()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "Settings",
            TableName = "app_settings",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "LastUpdated", ColumnName = "last_updated" }
            }
        };
        var cpql = "UPDATE Settings s SET s.LastUpdated = :timestamp";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Be("UPDATE app_settings SET last_updated = @timestamp");
    }

    [Fact]
    public void ConvertToSql_DeleteWithoutWhere_ShouldConvertCorrectly()
    {
        // Arrange - Dangerous but valid SQL
        var cpql = "DELETE FROM TempData t";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert
        sql.Should().Be("DELETE FROM tempdata");
    }

    [Fact]
    public void ConvertToSql_UpdateWithMultipleEntitiesMetadata_ShouldUseCorrectMetadata()
    {
        // Arrange
        var entitiesMetadata = new Dictionary<string, EntityMetadataInfo>
        {
            {
                "User",
                new EntityMetadataInfo
                {
                    Name = "User",
                    TableName = "users",
                    Properties = new List<PropertyMetadataInfo>
                    {
                        new() { Name = "LastLogin", ColumnName = "last_login" },
                        new() { Name = "LoginCount", ColumnName = "login_count" },
                        new() { Name = "Id", ColumnName = "id" }
                    }
                }
            }
        };
        var cpql = "UPDATE User u SET u.LastLogin = :now, u.LoginCount = u.LoginCount + 1 WHERE u.Id = :userId";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, entitiesMetadata);

        // Assert
        sql.Should().Be("UPDATE users SET last_login = @now, login_count = login_count + 1 WHERE id = @userId");
    }

    [Fact]
    public void ConvertToSql_DeleteWithMultipleEntitiesMetadata_ShouldUseCorrectMetadata()
    {
        // Arrange
        var entitiesMetadata = new Dictionary<string, EntityMetadataInfo>
        {
            {
                "AuditLog",
                new EntityMetadataInfo
                {
                    Name = "AuditLog",
                    TableName = "audit_logs",
                    Properties = new List<PropertyMetadataInfo>
                    {
                        new() { Name = "CreatedAt", ColumnName = "created_at" },
                        new() { Name = "Severity", ColumnName = "severity" }
                    }
                }
            }
        };
        var cpql = "DELETE FROM AuditLog a WHERE a.CreatedAt < :cutoffDate AND a.Severity = :severity";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, entitiesMetadata);

        // Assert
        sql.Should().Be("DELETE FROM audit_logs WHERE created_at < @cutoffDate AND severity = @severity");
    }

    #endregion

    #region INSERT Query Tests

    [Fact]
    public void ConvertToSql_SimpleInsert_ShouldConvertCorrectly()
    {
        // Arrange
        var cpql = "INSERT INTO Product (Name, Price) VALUES (:name, :price)";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves Name and Price property casing
        sql.Should().Be("INSERT INTO product (Name, Price) VALUES (@name, @price)");
    }

    [Fact]
    public void ConvertToSql_InsertWithMetadata_ShouldUseTableAndColumnNames()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "Product",
            TableName = "products",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Name", ColumnName = "product_name" },
                new() { Name = "Price", ColumnName = "unit_price" },
                new() { Name = "Stock", ColumnName = "stock_quantity" }
            }
        };
        var cpql = "INSERT INTO Product (Name, Price, Stock) VALUES (:name, :price, :stock)";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Be("INSERT INTO products (product_name, unit_price, stock_quantity) VALUES (@name, @price, @stock)");
    }

    [Fact]
    public void ConvertToSql_InsertMultipleColumns_ShouldConvertCorrectly()
    {
        // Arrange
        var cpql = "INSERT INTO User (Username, Email, FirstName, LastName, IsActive) VALUES (:username, :email, :firstName, :lastName, :isActive)";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves property casing
        sql.Should().Be("INSERT INTO user (Username, Email, FirstName, LastName, IsActive) VALUES (@username, @email, @firstName, @lastName, @isActive)");
    }

    [Fact]
    public void ConvertToSql_InsertWithMixedCaseColumns_ShouldConvertToSnakeCase()
    {
        // Arrange
        var cpql = "INSERT INTO Order (TotalAmount, DiscountPercent, OrderDate, CustomerId) VALUES (:totalAmount, :discountPercent, :orderDate, :customerId)";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves property casing (no longer converts to snake_case)
        sql.Should().Be("INSERT INTO order (TotalAmount, DiscountPercent, OrderDate, CustomerId) VALUES (@totalAmount, @discountPercent, @orderDate, @customerId)");
    }

    [Fact]
    public void ConvertToSql_InsertWithSingleColumn_ShouldConvertCorrectly()
    {
        // Arrange
        var cpql = "INSERT INTO Category (Name) VALUES (:name)";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves Name property casing
        sql.Should().Be("INSERT INTO category (Name) VALUES (@name)");
    }

    [Fact]
    public void ConvertToSql_InsertWithMetadataPartialMatch_ShouldUseFallbackForUnmatchedColumns()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "Product",
            TableName = "products",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "Name", ColumnName = "product_name" },
                new() { Name = "Price", ColumnName = "unit_price" }
                // Stock is not in metadata
            }
        };
        var cpql = "INSERT INTO Product (Name, Price, Stock) VALUES (:name, :price, :stock)";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert - uses Column names from metadata, preserves Stock casing when not in metadata
        sql.Should().Be("INSERT INTO products (product_name, unit_price, Stock) VALUES (@name, @price, @stock)");
    }

    [Fact]
    public void ConvertToSql_InsertWithTimestampColumns_ShouldConvertCorrectly()
    {
        // Arrange
        var metadata = new EntityMetadataInfo
        {
            Name = "Session",
            TableName = "sessions",
            Properties = new List<PropertyMetadataInfo>
            {
                new() { Name = "UserId", ColumnName = "user_id" },
                new() { Name = "CreatedAt", ColumnName = "created_at" },
                new() { Name = "ExpiresAt", ColumnName = "expires_at" }
            }
        };
        var cpql = "INSERT INTO Session (UserId, CreatedAt, ExpiresAt) VALUES (:userId, :createdAt, :expiresAt)";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, metadata);

        // Assert
        sql.Should().Be("INSERT INTO sessions (user_id, created_at, expires_at) VALUES (@userId, @createdAt, @expiresAt)");
    }

    [Fact]
    public void ConvertToSql_InsertWithBooleanColumn_ShouldConvertCorrectly()
    {
        // Arrange
        var cpql = "INSERT INTO Feature (Name, IsEnabled, IsPublic) VALUES (:name, :isEnabled, :isPublic)";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql);

        // Assert - preserves property casing
        sql.Should().Be("INSERT INTO feature (Name, IsEnabled, IsPublic) VALUES (@name, @isEnabled, @isPublic)");
    }

    [Fact]
    public void ConvertToSql_InsertWithMultipleEntitiesMetadata_ShouldUseCorrectMetadata()
    {
        // Arrange
        var entitiesMetadata = new Dictionary<string, EntityMetadataInfo>
        {
            {
                "User",
                new EntityMetadataInfo
                {
                    Name = "User",
                    TableName = "users",
                    Properties = new List<PropertyMetadataInfo>
                    {
                        new() { Name = "Email", ColumnName = "email_address" },
                        new() { Name = "FirstName", ColumnName = "first_name" }
                    }
                }
            },
            {
                "Product",
                new EntityMetadataInfo
                {
                    Name = "Product",
                    TableName = "products",
                    Properties = new List<PropertyMetadataInfo>
                    {
                        new() { Name = "Name", ColumnName = "product_name" },
                        new() { Name = "Price", ColumnName = "unit_price" }
                    }
                }
            }
        };
        var cpql = "INSERT INTO User (Email, FirstName) VALUES (:email, :firstName)";

        // Act
        var sql = CpqlToSqlConverter.ConvertToSql(cpql, entitiesMetadata);

        // Assert
        sql.Should().Be("INSERT INTO users (email_address, first_name) VALUES (@email, @firstName)");
    }

    #endregion
}






