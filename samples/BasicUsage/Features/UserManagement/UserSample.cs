using BasicUsage;
using Microsoft.Extensions.DependencyInjection;
using NPA.Core.Core;
using System;
using System.Threading.Tasks;

namespace BasicUsage.Features.UserManagement;

public static class UserSample
{
    public static async Task RunAsync(IServiceProvider serviceProvider, string provider)
    {
        using var scope = serviceProvider.CreateScope();
        var entityManager = scope.ServiceProvider.GetRequiredService<EntityManager>();

        var user = new User
        {
            Username = $"{provider}_john_doe",
            Email = $"john.doe@{provider}.example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await entityManager.PersistAsync(user);
        Console.WriteLine($"[{provider}] User created with ID: {user.Id}");

        var foundUser = await entityManager.FindAsync<User>(user.Id);
        if (foundUser != null)
        {
            Console.WriteLine($"[{provider}] Found user: {foundUser.Username}");
        }
    }
}
