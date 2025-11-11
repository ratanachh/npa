namespace NPA.Samples.Core;

/// <summary>
/// Represents a runnable sample that demonstrates a feature of the NPA framework.
/// </summary>
public interface ISample
{
    /// <summary>
    /// Gets the display name of the sample.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what the sample demonstrates.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Runs the sample logic asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RunAsync();
}
