using NPA.Core.Annotations;
using NPA.Samples.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BasicUsage.Samples;

/// <summary>
/// Demonstrates Phase 4.2 Query Method Generation features including:
/// - Convention-based method name parsing with OrderBy support
/// - Pagination attributes
/// - Complex ordering with multiple columns
/// </summary>
public class QueryMethodGenerationSample : ISample
{
    public string Name => "Query Method Generation";
    
    public string Description => "Demonstrates OrderBy conventions and pagination features";

    public async Task RunAsync()
    {
        Console.WriteLine("=== Query Method Generation Sample ===\n");

        // Note: This sample demonstrates the source generator features.
        // The repository methods are generated at compile-time based on method names and attributes.

        await DemonstrateOrderByConventions();
        await DemonstratePaginationAttributes();
        await DemonstrateComplexOrdering();

        Console.WriteLine("\n=== Sample Complete ===\n");
        
        await Task.CompletedTask;
    }

    private static Task DemonstrateOrderByConventions()
    {
        Console.WriteLine("1. OrderBy Convention-Based Methods");
        Console.WriteLine("   The generator parses method names like:");
        Console.WriteLine("   - FindByStatusOrderByName -> SELECT * FROM users WHERE status = @status ORDER BY name ASC");
        Console.WriteLine("   - FindByStatusOrderByNameDesc -> ... ORDER BY name DESC");
        Console.WriteLine("   - GetAllOrderByCreatedAtDesc -> SELECT * FROM users ORDER BY created_at DESC");
        Console.WriteLine();
        return Task.CompletedTask;
    }

    private static Task DemonstratePaginationAttributes()
    {
        Console.WriteLine("2. Pagination Attributes");
        Console.WriteLine("   Use [Paginated] attribute for automatic pagination:");
        Console.WriteLine("   - [Paginated(PageSize = 20)]");
        Console.WriteLine("   - [Paginated(PageSize = 10, MaxPageSize = 100)]");
        Console.WriteLine("   - [Paginated(IncludeTotalCount = true)]");
        Console.WriteLine();
        return Task.CompletedTask;
    }

    private static Task DemonstrateComplexOrdering()
    {
        Console.WriteLine("3. Complex Multi-Column Ordering");
        Console.WriteLine("   Method names support multiple OrderBy clauses:");
        Console.WriteLine("   - FindByStatusOrderByNameDescThenCreatedAtAsc");
        Console.WriteLine("     -> WHERE status = @status ORDER BY name DESC, created_at ASC");
        Console.WriteLine();
        Console.WriteLine("   Or use [OrderBy] attributes:");
        Console.WriteLine("   - [OrderBy(\"Name\", Direction = SortDirection.Descending, Priority = 1)]");
        Console.WriteLine("   - [OrderBy(\"CreatedAt\", Direction = SortDirection.Ascending, Priority = 2)]");
        Console.WriteLine();
        Console.WriteLine("Example repository interface:");
        Console.WriteLine(@"
    [Repository]
    public interface IUserRepository : IRepository<User, int>
    {
        // Convention-based ordering
        Task<IEnumerable<User>> FindAllOrderByName();
        Task<IEnumerable<User>> FindAllOrderByNameDesc();
        Task<IEnumerable<User>> FindByStatusOrderByCreatedAt(string status);
        
        // Multiple order columns
        Task<IEnumerable<User>> FindAllOrderByNameDescThenCreatedAtAsc();
        
        // With pagination
        [Paginated(PageSize = 10)]
        Task<IEnumerable<User>> GetAllPaged();
        
        // Attribute-based ordering
        [OrderBy(""Name"", Direction = SortDirection.Descending)]
        Task<IEnumerable<User>> GetAllSortedByName();
    }
");
        return Task.CompletedTask;
    }
}

