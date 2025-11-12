using System.Text;

namespace NPA.CLI.Services;

/// <summary>
/// Implementation of project template service.
/// </summary>
public class ProjectTemplateService : IProjectTemplateService
{
    public async Task CreateProjectAsync(string template, string projectName, string outputPath, ProjectOptions options)
    {
        Directory.CreateDirectory(outputPath);

        switch (template.ToLower())
        {
            case "console":
                await CreateConsoleProjectAsync(projectName, outputPath, options);
                break;
            case "webapi":
                await CreateWebApiProjectAsync(projectName, outputPath, options);
                break;
            case "classlib":
                await CreateClassLibraryProjectAsync(projectName, outputPath, options);
                break;
            default:
                throw new ArgumentException($"Unknown template: {template}");
        }
    }

    public Task<IEnumerable<string>> GetAvailableTemplatesAsync()
    {
        var templates = new[] { "console", "webapi", "classlib" };
        return Task.FromResult<IEnumerable<string>>(templates);
    }

    private async Task CreateConsoleProjectAsync(string projectName, string outputPath, ProjectOptions options)
    {
        // Create .csproj
        var csproj = GenerateProjectFile(projectName, "Exe");
        await File.WriteAllTextAsync(Path.Combine(outputPath, $"{projectName}.csproj"), csproj);

        // Create Program.cs
        var program = GenerateProgramCs(projectName, options, isWebApi: false);
        await File.WriteAllTextAsync(Path.Combine(outputPath, "Program.cs"), program);

        // Create Entities directory
        Directory.CreateDirectory(Path.Combine(outputPath, "Entities"));

        // Create Repositories directory
        Directory.CreateDirectory(Path.Combine(outputPath, "Repositories"));

        // Create sample entity if requested
        if (options.IncludeSamples)
        {
            await CreateSampleEntityAsync(projectName, outputPath);
        }
    }

    private async Task CreateWebApiProjectAsync(string projectName, string outputPath, ProjectOptions options)
    {
        // Create .csproj with web packages
        var csproj = GenerateWebApiProjectFile(projectName);
        await File.WriteAllTextAsync(Path.Combine(outputPath, $"{projectName}.csproj"), csproj);

        // Create Program.cs
        var program = GenerateProgramCs(projectName, options, isWebApi: true);
        await File.WriteAllTextAsync(Path.Combine(outputPath, "Program.cs"), program);

        // Create Controllers directory
        Directory.CreateDirectory(Path.Combine(outputPath, "Controllers"));

        // Create Entities directory
        Directory.CreateDirectory(Path.Combine(outputPath, "Entities"));

        // Create Repositories directory
        Directory.CreateDirectory(Path.Combine(outputPath, "Repositories"));

        // Create appsettings.json
        var appsettings = GenerateAppSettings(options);
        await File.WriteAllTextAsync(Path.Combine(outputPath, "appsettings.json"), appsettings);

        // Create sample controller if requested
        if (options.IncludeSamples)
        {
            await CreateSampleControllerAsync(projectName, outputPath);
            await CreateSampleEntityAsync(projectName, outputPath);
        }
    }

    private async Task CreateClassLibraryProjectAsync(string projectName, string outputPath, ProjectOptions options)
    {
        // Create .csproj
        var csproj = GenerateProjectFile(projectName, "Library");
        await File.WriteAllTextAsync(Path.Combine(outputPath, $"{projectName}.csproj"), csproj);

        // Create Entities directory
        Directory.CreateDirectory(Path.Combine(outputPath, "Entities"));

        // Create Repositories directory
        Directory.CreateDirectory(Path.Combine(outputPath, "Repositories"));
    }

    private string GenerateProjectFile(string projectName, string outputType)
    {
        return $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>{outputType}</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""NPA.Core"" Version=""0.1.0"" />
    <PackageReference Include=""NPA.Extensions"" Version=""0.1.0"" />
    <PackageReference Include=""NPA.Generators"" Version=""0.1.0"" />
    <PackageReference Include=""Microsoft.Extensions.DependencyInjection"" Version=""7.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Logging"" Version=""7.0.0"" />
  </ItemGroup>
</Project>";
    }

    private string GenerateWebApiProjectFile(string projectName)
    {
        return $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""NPA.Core"" Version=""0.1.0"" />
    <PackageReference Include=""NPA.Extensions"" Version=""0.1.0"" />
    <PackageReference Include=""NPA.Generators"" Version=""0.1.0"" />
    <PackageReference Include=""Microsoft.AspNetCore.OpenApi"" Version=""8.0.0"" />
    <PackageReference Include=""Swashbuckle.AspNetCore"" Version=""6.5.0"" />
  </ItemGroup>
</Project>";
    }

    private string GenerateProgramCs(string projectName, ProjectOptions options, bool isWebApi)
    {
        if (isWebApi)
        {
            return $@"using NPA.Core;
using NPA.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add NPA services
builder.Services.AddNpa(npa =>
{{
    npa.UseConnectionString(""{options.ConnectionString}"");
    npa.UseDatabaseProvider(""{options.DatabaseProvider}"");
}});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{{
    app.UseSwagger();
    app.UseSwaggerUI();
}}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
";
        }
        else
        {
            return $@"using NPA.Core;
using NPA.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{{
    // Add NPA services
    services.AddNpa(npa =>
    {{
        npa.UseConnectionString(""{options.ConnectionString}"");
        npa.UseDatabaseProvider(""{options.DatabaseProvider}"");
    }});
}});

var host = builder.Build();

// Your application logic here
Console.WriteLine(""NPA application started"");

await host.RunAsync();
";
        }
    }

    private string GenerateAppSettings(ProjectOptions options)
    {
        return $@"{{
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }}
  }},
  ""AllowedHosts"": ""*"",
  ""ConnectionStrings"": {{
    ""DefaultConnection"": ""{options.ConnectionString}""
  }},
  ""NPA"": {{
    ""DatabaseProvider"": ""{options.DatabaseProvider}""
  }}
}}";
    }

    private async Task CreateSampleEntityAsync(string projectName, string outputPath)
    {
        var entity = @"using NPA.Core.Attributes;

namespace " + projectName + @".Entities;

[Entity]
[Table(""users"")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column(""id"")]
    public long Id { get; set; }

    [Column(""username"", IsNullable = false, Length = 50)]
    public string Username { get; set; } = string.Empty;

    [Column(""email"", IsNullable = false, IsUnique = true)]
    public string Email { get; set; } = string.Empty;

    [Column(""created_at"")]
    public DateTime CreatedAt { get; set; }

    [Column(""is_active"")]
    public bool IsActive { get; set; } = true;
}
";
        await File.WriteAllTextAsync(Path.Combine(outputPath, "Entities", "User.cs"), entity);
    }

    private async Task CreateSampleControllerAsync(string projectName, string outputPath)
    {
        var controller = @"using Microsoft.AspNetCore.Mvc;
using NPA.Core;
using " + projectName + @".Entities;

namespace " + projectName + @".Controllers;

[ApiController]
[Route(""api/[controller]"")]
public class UsersController : ControllerBase
{
    private readonly IEntityManager _entityManager;

    public UsersController(IEntityManager entityManager)
    {
        _entityManager = entityManager;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAll()
    {
        var users = await _entityManager.CreateQuery<User>()
            .Where(u => u.IsActive)
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet(""{id}"")]
    public async Task<ActionResult<User>> GetById(long id)
    {
        var user = await _entityManager.FindAsync<User>(id);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> Create(User user)
    {
        user.CreatedAt = DateTime.UtcNow;
        await _entityManager.PersistAsync(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut(""{id}"")]
    public async Task<IActionResult> Update(long id, User user)
    {
        if (id != user.Id)
        {
            return BadRequest();
        }

        await _entityManager.MergeAsync(user);
        return NoContent();
    }

    [HttpDelete(""{id}"")]
    public async Task<IActionResult> Delete(long id)
    {
        var user = await _entityManager.FindAsync<User>(id);
        if (user == null)
        {
            return NotFound();
        }

        await _entityManager.RemoveAsync(user);
        return NoContent();
    }
}
";
        await File.WriteAllTextAsync(Path.Combine(outputPath, "Controllers", "UsersController.cs"), controller);
    }
}
