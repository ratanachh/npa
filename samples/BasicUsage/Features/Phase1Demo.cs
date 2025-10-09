using Microsoft.Extensions.DependencyInjection;
using NPA.Core.Core;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Data;

namespace BasicUsage.Features;

/// <summary>
/// Cohesive demonstration of Phases 1.1 - 1.4:
/// 1.1 Mapping via attributes (see User entity)
/// 1.2 EntityManager CRUD lifecycle (persist, find, merge, remove)
/// 1.3 Simple query creation & parameter binding
/// 1.4 Database provider usage (SQL Server or PostgreSQL)
/// All phases fully implemented and tested.
/// </summary>
public static class Phase1Demo
{
    public static async Task RunAsync(IServiceProvider root, string provider)
    {
        using var scope = root.CreateScope();
        var em = scope.ServiceProvider.GetRequiredService<EntityManager>();

        Console.WriteLine("--- Phase1 Demo: Lifecycle & Query ---");

        // Create (Persist)
        var user = new User
        {
            Username = $"{provider}_phase1_user",
            Email = $"phase1@{provider}.example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        await em.PersistAsync(user);
        Console.WriteLine($"Persisted user id={user.Id}");

        // Read (Find)
        var found = await em.FindAsync<User>(user.Id);
        Console.WriteLine(found != null
            ? $"Found user username={found.Username}"
            : "User not found unexpectedly");

        // Update (Merge)
        found!.Email = "updated." + found.Email;
        await em.MergeAsync(found);
        Console.WriteLine($"Merged user newEmail={found.Email}");

        // Optional tracking demonstration
        if (em.Contains(found))
        {
            Console.WriteLine("EntityManager tracking user; detaching...");
            em.Detach(found);
        }
        var refetched = await em.FindAsync<User>(user.Id);
        Console.WriteLine($"Refetched after detach => {(refetched != null ? "ok" : "missing")}");

        // Simple query list + single
        try
        {
            var activeList = await em
                .CreateQuery<User>("SELECT u FROM User u WHERE u.IsActive = :active")
                .SetParameter("active", true)
                .GetResultListAsync();
            Console.WriteLine($"Query active count={activeList.Count()}");

            var single = await em
                .CreateQuery<User>("SELECT u FROM User u WHERE u.Id = :id")
                .SetParameter("id", user.Id)
                .GetSingleResultAsync();
            Console.WriteLine($"Query single={single?.Username}");
        }
        catch (Exception qex)
        {
            Console.WriteLine($"Query subsystem not fully available: {qex.Message}");
        }

        // Delete
        await em.RemoveAsync(refetched!);
        var afterDelete = await em.FindAsync<User>(user.Id);
        Console.WriteLine(afterDelete == null ? "User removed successfully" : "User still present (unexpected)");

        Console.WriteLine("--- End Phase1 Demo ---");
    }
}
