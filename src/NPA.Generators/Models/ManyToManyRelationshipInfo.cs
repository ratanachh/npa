namespace NPA.Generators.Models;

internal class ManyToManyRelationshipInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public string CollectionElementType { get; set; } = string.Empty;
    public string JoinTableName { get; set; } = string.Empty;
    public string JoinTableSchema { get; set; } = string.Empty;
    public string[] JoinColumns { get; set; } = Array.Empty<string>();
    public string[] InverseJoinColumns { get; set; } = Array.Empty<string>();
    public string MappedBy { get; set; } = string.Empty;
}

