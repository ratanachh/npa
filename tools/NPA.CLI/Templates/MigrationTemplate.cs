namespace NPA.CLI.Templates;

/// <summary>
/// Template for generating migration classes.
/// </summary>
public class MigrationTemplate
{
    public string Generate(string migrationName, string namespaceName)
    {
        var version = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var className = $"{migrationName}Migration";

        return $@"using NPA.Migrations;

namespace {namespaceName};

/// <summary>
/// Migration: {migrationName}
/// Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
/// </summary>
public class {className} : IMigration
{{
    public int Version => {version};
    
    public string Description => ""{migrationName}"";

    public async Task UpAsync(IMigrationContext context)
    {{
        // TODO: Implement migration logic here
        // Example:
        // await context.ExecuteAsync(@""
        //     CREATE TABLE example_table (
        //         id BIGINT PRIMARY KEY IDENTITY(1,1),
        //         name NVARCHAR(100) NOT NULL,
        //         created_at DATETIME2 NOT NULL DEFAULT GETUTCDATE()
        //     )
        // "");
        
        await Task.CompletedTask;
    }}

    public async Task DownAsync(IMigrationContext context)
    {{
        // TODO: Implement rollback logic here
        // Example:
        // await context.ExecuteAsync(""DROP TABLE example_table"");
        
        await Task.CompletedTask;
    }}
}}
";
    }
}
