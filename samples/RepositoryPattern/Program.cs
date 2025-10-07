using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NPA.Core;

namespace RepositoryPattern;

class Program
{
    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting Repository Pattern Demo...");

        await RunRepositoryPatternDemo(host.Services);
        
        logger.LogInformation("Repository Pattern Demo completed.");
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // TODO: Configure NPA services and repositories
                services.AddLogging();
                services.AddScoped<IUserRepository, UserRepository>();
            });

    static async Task RunRepositoryPatternDemo(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        var userRepository = services.GetRequiredService<IUserRepository>();
        
        logger.LogInformation("Demonstrating repository pattern...");
        
        // TODO: Demonstrate repository operations
        // var users = await userRepository.GetAllAsync();
        // var user = await userRepository.GetByIdAsync(1);
        // await userRepository.CreateAsync(new User { Name = "John Doe" });
        
        await Task.CompletedTask;
    }
}

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task DeleteAsync(int id);
}

public class UserRepository : IUserRepository
{
    // TODO: Implement repository methods using NPA
    public Task<IEnumerable<User>> GetAllAsync() => Task.FromResult(Enumerable.Empty<User>());
    public Task<User?> GetByIdAsync(int id) => Task.FromResult<User?>(null);
    public Task<User> CreateAsync(User user) => Task.FromResult(user);
    public Task<User> UpdateAsync(User user) => Task.FromResult(user);
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}