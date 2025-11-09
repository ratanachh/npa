using NPA.Core.Annotations;
using NPA.Core.Repositories;
using NPA.Samples.Core;
using NPA.Samples.Entities;

namespace NPA.Samples.Features;

public class AdvancedRepositoryGeneratorSample : ISample
{
    public string Name => "Advanced Repository Generator (Phase 4.1)";
    public string Description => "Demonstrates advanced repository generation with custom queries, stored procedures, and method naming conventions.";

    public Task RunAsync()
    {
        Console.WriteLine("=== Advanced Repository Generator Features (Phase 4.1) ===\n");

        Console.WriteLine("Phase 4.1 introduces powerful code generation capabilities:\n");

        Console.WriteLine("1. CUSTOM QUERY ATTRIBUTE");
        Console.WriteLine("   Define custom SQL queries with the [Query] attribute:");
        Console.WriteLine("   ");
        Console.WriteLine("   [Query(\"SELECT * FROM users WHERE email = @email\")]");
        Console.WriteLine("   Task<User?> FindByEmailAsync(string email);");
        Console.WriteLine();

        Console.WriteLine("2. STORED PROCEDURE SUPPORT");
        Console.WriteLine("   Call stored procedures with the [StoredProcedure] attribute:");
        Console.WriteLine("   ");
        Console.WriteLine("   [StoredProcedure(\"sp_GetActiveUsers\")]");
        Console.WriteLine("   Task<IEnumerable<User>> GetActiveUsersAsync();");
        Console.WriteLine();

        Console.WriteLine("3. METHOD NAMING CONVENTIONS");
        Console.WriteLine("   Generate SQL automatically from method names:");
        Console.WriteLine("   ");
        Console.WriteLine("   - FindByEmail(string email)           => SELECT * FROM users WHERE email = @email");
        Console.WriteLine("   - CountByStatus(string status)        => SELECT COUNT(*) FROM users WHERE status = @status");
        Console.WriteLine("   - ExistsByUsername(string username)   => SELECT COUNT(1) FROM users WHERE username = @username");
        Console.WriteLine("   - DeleteByEmail(string email)         => DELETE FROM users WHERE email = @email");
        Console.WriteLine();

        Console.WriteLine("4. MULTI-PROPERTY QUERIES");
        Console.WriteLine("   Handle multiple properties with 'And' keyword:");
        Console.WriteLine("   ");
        Console.WriteLine("   FindByEmailAndStatus(string email, string status)");
        Console.WriteLine("   => SELECT * FROM users WHERE email = @email AND status = @status");
        Console.WriteLine();

        Console.WriteLine("5. BULK OPERATIONS");
        Console.WriteLine("   Optimize batch operations with [BulkOperation]:");
        Console.WriteLine("   ");
        Console.WriteLine("   [BulkOperation(BatchSize = 1000)]");
        Console.WriteLine("   Task<int> BulkInsertAsync(IEnumerable<User> users);");
        Console.WriteLine();

        Console.WriteLine("6. MULTI-MAPPING (Advanced)");
        Console.WriteLine("   Map complex relationships with [MultiMapping]:");
        Console.WriteLine("   ");
        Console.WriteLine("   [MultiMapping(\"id\", SplitOn = \"address_id\")]");
        Console.WriteLine("   [Query(\"SELECT u.*, a.* FROM users u LEFT JOIN addresses a ON u.id = a.user_id\")]");
        Console.WriteLine("   Task<User> GetUserWithAddressAsync(long id);");
        Console.WriteLine();

        Console.WriteLine("SUPPORTED METHOD NAME PREFIXES:");
        Console.WriteLine("   - Find/Get/Query/Search     => SELECT queries");
        Console.WriteLine("   - Count                     => COUNT queries");
        Console.WriteLine("   - Exists/Has/Is/Contains    => EXISTS checks (returns bool)");
        Console.WriteLine("   - Delete/Remove             => DELETE operations");
        Console.WriteLine("   - Update/Modify             => UPDATE operations");
        Console.WriteLine("   - Insert/Add/Save/Create    => INSERT operations");
        Console.WriteLine();

        Console.WriteLine("EXAMPLE REPOSITORY INTERFACE:");
        Console.WriteLine("===============================");
        Console.WriteLine();
        Console.WriteLine("[Repository]");
        Console.WriteLine("public interface IUserRepository : IRepository<User, long>");
        Console.WriteLine("{");
        Console.WriteLine("    // Convention-based query");
        Console.WriteLine("    Task<IEnumerable<User>> FindByStatusAsync(string status);");
        Console.WriteLine();
        Console.WriteLine("    // Custom SQL query");
        Console.WriteLine("    [Query(\"SELECT * FROM users WHERE created_at > @date ORDER BY created_at DESC\")]");
        Console.WriteLine("    Task<IEnumerable<User>> GetRecentUsersAsync(DateTime date);");
        Console.WriteLine();
        Console.WriteLine("    // Stored procedure");
        Console.WriteLine("    [StoredProcedure(\"sp_ArchiveInactiveUsers\")]");
        Console.WriteLine("    Task<int> ArchiveInactiveUsersAsync(int daysInactive);");
        Console.WriteLine();
        Console.WriteLine("    // Exists check");
        Console.WriteLine("    Task<bool> ExistsByEmailAsync(string email);");
        Console.WriteLine();
        Console.WriteLine("    // Bulk operation");
        Console.WriteLine("    [BulkOperation]");
        Console.WriteLine("    Task<int> BulkInsertUsersAsync(IEnumerable<User> users);");
        Console.WriteLine("}");
        Console.WriteLine();

        Console.WriteLine("The generator automatically creates the implementation class");
        Console.WriteLine("with all method bodies generated at compile-time!");
        Console.WriteLine();
        Console.WriteLine("===============================");
        Console.WriteLine("See the generated code in:");
        Console.WriteLine("  obj/Debug/net8.0/generated/NPA.Generators.RepositoryGenerator/");
        Console.WriteLine("===============================");

        return Task.CompletedTask;
    }
}

// Example repository interface showing new features
[Repository]
public interface IAdvancedUserRepository : IRepository<User, long>
{
    // Convention-based: generates SELECT * FROM users WHERE status = @status
    Task<IEnumerable<User>> FindByStatusAsync(string status);

    // Convention-based: generates SELECT COUNT(*) FROM users WHERE status = @status
    Task<int> CountByStatusAsync(string status);

    // Convention-based: generates SELECT COUNT(1) FROM users WHERE email = @email (returns > 0)
    Task<bool> ExistsByEmailAsync(string email);

    // Custom query with SQL
    [Query("SELECT * FROM users WHERE created_at > @date ORDER BY created_at DESC LIMIT @limit")]
    Task<IEnumerable<User>> GetRecentUsersAsync(DateTime date, int limit);

    // Custom query returning single result
    [Query("SELECT * FROM users WHERE email = @email")]
    Task<User?> GetByEmailAsync(string email);
}
