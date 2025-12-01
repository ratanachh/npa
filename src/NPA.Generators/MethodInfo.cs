using Microsoft.CodeAnalysis;

namespace NPA.Generators;

internal class MethodInfo
{
    public string Name { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public List<ParameterInfo> Parameters { get; set; } = new();
    public MethodAttributeInfo Attributes { get; set; } = new();
    public IMethodSymbol? Symbol { get; set; }
}