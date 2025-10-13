using NPA.Samples.Core;

namespace NPA.Samples;

class Program
{
    static async Task Main(string[] args)
    {
        var sampleRunner = new SampleRunner();
        await sampleRunner.RunAsync();
    }
}
