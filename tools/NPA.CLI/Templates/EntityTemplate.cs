namespace NPA.CLI.Templates;

/// <summary>
/// Template for generating entity classes.
/// </summary>
public class EntityTemplate
{
    public string Generate(string entityName, string namespaceName, string tableName)
    {
        var lowerEntityName = entityName.ToLower();
        var lowerTableName = tableName.ToLower();

        return $@"using NPA.Core.Attributes;

namespace {namespaceName};

/// <summary>
/// Entity class for {entityName}.
/// </summary>
[Entity]
[Table(""{lowerTableName}"")]
public class {entityName}
{{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column(""id"")]
    public long Id {{ get; set; }}

    [Column(""name"", IsNullable = false, Length = 100)]
    public string Name {{ get; set; }} = string.Empty;

    [Column(""created_at"")]
    public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;

    [Column(""updated_at"")]
    public DateTime? UpdatedAt {{ get; set; }}

    // Add additional properties here
}}
";
    }
}
