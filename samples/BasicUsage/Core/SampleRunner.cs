using System.Reflection;

namespace NPA.Samples.Core;

/// <summary>
/// Discovers and runs available samples.
/// </summary>
public class SampleRunner
{
    private readonly IReadOnlyList<ISample> _samples;

    public SampleRunner()
    {
        _samples = DiscoverSamples();
    }

    /// <summary>
    /// Displays a menu of available samples and runs the one selected by the user.
    /// </summary>
    public async Task RunAsync()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== NPA Framework Samples ===");
            Console.WriteLine("Please choose a sample to run:");

            for (int i = 0; i < _samples.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {_samples[i].Name}");
                Console.WriteLine($"     {_samples[i].Description}");
            }

            Console.WriteLine("\n  A. Run All Samples");
            Console.WriteLine("  Q. Quit");
            Console.Write("\nEnter your choice: ");

            var choice = Console.ReadLine();

            if (string.Equals(choice, "q", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (string.Equals(choice, "a", StringComparison.OrdinalIgnoreCase))
            {
                await RunAllSamplesAsync();
                continue;
            }

            if (int.TryParse(choice, out var index) && index > 0 && index <= _samples.Count)
            {
                await RunSampleAsync(_samples[index - 1]);
            }
        }
    }

    private async Task RunAllSamplesAsync()
    {
        Console.Clear();
        Console.WriteLine("--- Running All Samples ---");

        foreach (var sample in _samples)
        {
            await RunSampleAsync(sample);
        }

        Console.WriteLine("\n--- All Samples Finished. Press any key to return to the menu. ---");
        Console.ReadKey();
    }

    private async Task RunSampleAsync(ISample sample)
    {
        Console.WriteLine($"\n--- Running Sample: {sample.Name} ---");
        await sample.RunAsync();
        Console.WriteLine($"--- Finished Sample: {sample.Name} ---");
    }

    private IReadOnlyList<ISample> DiscoverSamples()
    {
        var sampleType = typeof(ISample);
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(p => sampleType.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract)
            .Select(t => Activator.CreateInstance(t) as ISample)
            .Where(s => s != null)
            .OrderBy(s => s!.Name)
            .ToList()!;
    }
}
