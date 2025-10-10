using ConsoleAppSync.Features;

namespace ConsoleAppSync;

/// <summary>
/// Console Application demonstrating SYNCHRONOUS methods in NPA.
/// Uses Testcontainers for fully isolated, self-contained demo.
/// No external database required - runs SQL Server in Docker!
/// 
/// This sample follows the same structure as BasicUsage provider runners.
/// </summary>
internal static class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("=== NPA Console App - Synchronous Methods Demo ===");
        Console.WriteLine("Using Testcontainers (SQL Server in Docker)");
        Console.WriteLine("Following best practices from BasicUsage samples\n");

        try
        {
            await SyncMethodsRunner.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Fatal Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}

