using JsonApiDotNetCore.Configuration;
using NoEntityFrameworkExample.Data;
using NoEntityFrameworkExample.Models;
using NoEntityFrameworkExample.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

string? connectionString = builder.Configuration["Data:DefaultConnection"];
builder.Services.AddNpgsql<AppDbContext>(connectionString);

builder.Services.AddJsonApi(options => options.Namespace = "api/v1", resources: resourceGraphBuilder => resourceGraphBuilder.Add<WorkItem, int>());

builder.Services.AddResourceService<WorkItemService>();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.

app.UseRouting();
app.UseJsonApi();
app.MapControllers();

await CreateDatabaseAsync(app.Services);

app.Run();

static async Task CreateDatabaseAsync(IServiceProvider serviceProvider)
{
    await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}
