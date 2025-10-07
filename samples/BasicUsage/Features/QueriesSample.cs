using Microsoft.Extensions.DependencyInjection;
using NPA.Core.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BasicUsage.Features;

/// <summary>
/// Demonstrates query usage: list retrieval, single result, parameter binding.
/// </summary>
public static class QueriesSample
{
    public static async Task RunAsync(IServiceProvider serviceProvider, string provider)
    {
        using var scope = serviceProvider.CreateScope();
        var entityManager = scope.ServiceProvider.GetRequiredService<EntityManager>();

        // Seed a couple of users if none exist (best-effort / non-transactional here)
        for (int i = 0; i < 2; i++)
        {
            var temp = new User
            {
                Username = $"{provider}_query_demo_{Guid.NewGuid():N}".Substring(0, 30),
                Email = $"query{i}@{provider}.example.com",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                IsActive = i % 2 == 0
            };
            await entityManager.PersistAsync(temp);
        }

        try
        {
            var activeUsers = await entityManager
                .CreateQuery<User>("SELECT u FROM User u WHERE u.IsActive = :active")
                .SetParameter("active", true)
                .GetResultListAsync();
            Console.WriteLine($"[query] Active users: {activeUsers.Count()}");

            var firstActive = await entityManager
                .CreateQuery<User>("SELECT u FROM User u WHERE u.IsActive = :active")
                .SetParameter("active", true)
                .GetSingleResultAsync();
            Console.WriteLine($"[query] First active user (or null): {firstActive?.Username}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[query] Query infrastructure exception: {ex.Message}");
        }
    }
}
