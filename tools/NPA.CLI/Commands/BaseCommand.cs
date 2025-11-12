using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace NPA.CLI.Commands;

/// <summary>
/// Base class for all CLI commands providing common functionality.
/// </summary>
public abstract class BaseCommand : Command
{
    protected readonly ILogger Logger;

    protected BaseCommand(string name, string description, ILogger logger) 
        : base(name, description)
    {
        Logger = logger;
    }

    protected void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ {message}");
        Console.ResetColor();
    }

    protected void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ {message}");
        Console.ResetColor();
    }

    protected void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠ {message}");
        Console.ResetColor();
    }

    protected void WriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"ℹ {message}");
        Console.ResetColor();
    }

    protected string Prompt(string message, string? defaultValue = null)
    {
        if (defaultValue != null)
        {
            Console.Write($"{message} [{defaultValue}]: ");
        }
        else
        {
            Console.Write($"{message}: ");
        }

        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? (defaultValue ?? string.Empty) : input;
    }

    protected bool Confirm(string message, bool defaultValue = false)
    {
        var defaultText = defaultValue ? "Y/n" : "y/N";
        Console.Write($"{message} [{defaultText}]: ");
        
        var input = Console.ReadLine()?.Trim().ToLower();
        
        if (string.IsNullOrWhiteSpace(input))
        {
            return defaultValue;
        }

        return input == "y" || input == "yes";
    }
}
