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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.TryAddSingleton<ISystemClock, SystemClock>();

DatabaseProvider databaseProvider = GetDatabaseProvider(builder.Configuration);
string connectionString = GetConnectionString(builder.Configuration, databaseProvider);

switch (databaseProvider)
{
    case DatabaseProvider.PostgreSql:
    {
        builder.Services.AddNpgsql<AppDbContext>(connectionString, optionsAction: SetDatabaseOptions);
        break;
    }
    case DatabaseProvider.MySql:
    {
        builder.Services.AddMySql<AppDbContext>(connectionString, ServerVersion.AutoDetect(connectionString), optionsAction: SetDatabaseOptions);
        break;
    }
    case DatabaseProvider.SqlServer:
    {
        builder.Services.AddSqlServer<AppDbContext>(connectionString, optionsAction: SetDatabaseOptions);
        break;
    }
}

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

static DatabaseProvider GetDatabaseProvider(IConfiguration configuration)
{
    return configuration.GetValue<DatabaseProvider>("DatabaseProvider");
}

static string GetConnectionString(IConfiguration configuration, DatabaseProvider databaseProvider)
{
    return configuration.GetConnectionString($"DapperExample{databaseProvider}")!;
}

static void SetDatabaseOptions(DbContextOptionsBuilder dbContextOptionsBuilder)
{
#if DEBUG
    dbContextOptionsBuilder.EnableSensitiveDataLogging();
    dbContextOptionsBuilder.EnableDetailedErrors();
#endif
}

static async Task CreateDatabaseAsync(IServiceProvider serviceProvider)
{
    await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}
