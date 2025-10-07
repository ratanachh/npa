using Microsoft.OpenApi.Models;

namespace WebApplication;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "NPA Web API Demo", 
                Version = "v1",
                Description = "Demonstration of NPA in a web application context"
            });
        });

        // TODO: Configure NPA services
        // builder.Services.AddNPA(options => {
        //     options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        //     options.Provider = DatabaseProvider.SqlServer;
        // });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "NPA Web API Demo v1");
                c.RoutePrefix = string.Empty; // Serve Swagger UI at root
            });
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}