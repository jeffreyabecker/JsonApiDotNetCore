using System.Text.Json.Serialization;
using DapperExample;
using DapperExample.AtomicOperations;
using DapperExample.Data;
using DapperExample.Models;
using DapperExample.Repositories;
using DapperExample.TranslationToSql.DataModel;
using JsonApiDotNetCore.AtomicOperations;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.TryAddSingleton<ISystemClock, SystemClock>();

string connectionString = GetConnectionString(builder.Configuration);

builder.Services.AddNpgsql<AppDbContext>(connectionString, optionsAction: options =>
{
#if DEBUG
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
#endif
});

builder.Services.AddScoped(typeof(IResourceRepository<,>), typeof(DapperRepository<,>));
builder.Services.AddScoped(typeof(IResourceWriteRepository<,>), typeof(DapperRepository<,>));
builder.Services.AddScoped(typeof(IResourceReadRepository<,>), typeof(DapperRepository<,>));

builder.Services.AddJsonApi(options =>
{
    options.AllowClientGeneratedIds = true;
    options.UseRelativeLinks = true;
    options.IncludeTotalResourceCount = true;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());

#if DEBUG
    options.IncludeExceptionStackTraceInErrors = true;
    options.SerializerOptions.WriteIndented = true;
#endif
}, discovery => discovery.AddCurrentAssembly(), resourceGraphBuilder =>
{
    resourceGraphBuilder.Add<TodoItem, long>();
    resourceGraphBuilder.Add<Person, long>();
    resourceGraphBuilder.Add<LoginAccount, long>();
    resourceGraphBuilder.Add<AccountRecovery, long>();
    resourceGraphBuilder.Add<Tag, long>();
    resourceGraphBuilder.Add<RgbColor, int?>();
});

builder.Services.AddScoped<IInverseNavigationResolver, FromEntitiesNavigationResolver>();
builder.Services.AddSingleton<FromEntitiesDataModelService>();
builder.Services.AddSingleton<IDataModelService>(serviceProvider => serviceProvider.GetRequiredService<FromEntitiesDataModelService>());
builder.Services.AddScoped<DapperTransactionFactory>();
builder.Services.AddScoped<IOperationsTransactionFactory>(serviceProvider => serviceProvider.GetRequiredService<DapperTransactionFactory>());

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.

app.UseRouting();
app.UseJsonApi();
app.MapControllers();

await CreateDatabaseAsync(app.Services);

app.Run();

static string GetConnectionString(IConfiguration configuration)
{
    string postgresPassword = Environment.GetEnvironmentVariable("PGPASSWORD") ?? "postgres";
    return configuration.GetConnectionString("DapperExampleDb")?.Replace("###", postgresPassword)!;
}

static async Task CreateDatabaseAsync(IServiceProvider serviceProvider)
{
    await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}
