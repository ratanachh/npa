using NPA.Providers.PostgreSql.Extensions;
using Testcontainers.PostgreSql;
using UdemyCloneSaaS.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Udemy Clone SaaS API", Version = "v1" });
});

// Configure PostgreSQL with Testcontainers
var postgresContainer = new PostgreSqlBuilder()
    .WithImage("postgres:16-alpine")
    .WithDatabase("udemy_clone")
    .WithUsername("postgres")
    .WithPassword("postgres")
    .WithPortBinding(5432, true)
    .Build();

// Start the container
await postgresContainer.StartAsync();

// Get the connection string from the container
var connectionString = postgresContainer.GetConnectionString();

// Register PostgreSQL provider with connection string
builder.Services.AddPostgreSqlProvider(connectionString);

// Register all NPA repositories using the generated extension method
// This automatically registers IEntityManager, BaseRepository, and all repositories
builder.Services.AddNPA();

// Register database initializer
builder.Services.AddTransient<DatabaseInitializer>();

var app = builder.Build();

// Initialize database schema and seed data
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await dbInitializer.InitializeAsync();
    await dbInitializer.SeedDataAsync();
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Udemy Clone SaaS API v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure container is disposed when application stops
app.Lifetime.ApplicationStopping.Register(() =>
{
    postgresContainer.StopAsync().GetAwaiter().GetResult();
    postgresContainer.DisposeAsync().AsTask().GetAwaiter().GetResult();
});

app.Run();
