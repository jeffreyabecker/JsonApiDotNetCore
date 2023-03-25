using System.Text.Json;
using DapperExample;
using DapperExample.Data;
using DapperExample.Models;
using DapperExample.Repositories;
using DapperExample.TranslationToSql.DataModel;
using FluentAssertions.Common;
using FluentAssertions.Extensions;
using JsonApiDotNetCore.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestBuildingBlocks;
using Xunit;
using Xunit.Abstractions;

namespace DapperTests;

// TODO: Run tests in parallel.

public sealed partial class SqlTests : IntegrationTest, IClassFixture<WebApplicationFactory<TodoItem>>
{
    private const string SqlServerClearAllTablesScript = @"
        EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
        EXEC sp_MSForEachTable 'SET QUOTED_IDENTIFIER ON; DELETE FROM ?';
        EXEC sp_MSForEachTable 'ALTER TABLE ? CHECK CONSTRAINT ALL';";

    private static readonly DateTimeOffset FrozenTime = 29.September(2018).At(16, 41, 56).AsUtc().ToDateTimeOffset();

    private readonly WebApplicationFactory<TodoItem> _factory;
    private readonly TestFakers _fakers = new();
    private readonly SqlTextAdapter _adapter;

    protected override JsonSerializerOptions SerializerOptions
    {
        get
        {
            var options = _factory.Services.GetRequiredService<IJsonApiOptions>();
            return options.SerializerOptions;
        }
    }

    public SqlTests(WebApplicationFactory<TodoItem> factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder.Services.AddSingleton<ILoggerProvider>(_ => new XUnitLoggerProvider(testOutputHelper));
            });

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ISystemClock>(new FrozenSystemClock
                {
                    UtcNow = FrozenTime
                });

                services.AddSingleton<SqlCaptureStore>();
            });
        });

        var dataModelService = _factory.Services.GetRequiredService<IDataModelService>();
        _adapter = new SqlTextAdapter(dataModelService.DatabaseProvider);
    }

    protected override HttpClient CreateClient()
    {
        return _factory.CreateClient();
    }

    private async Task RunOnDatabaseAsync(Func<AppDbContext, Task> asyncAction)
    {
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await asyncAction(dbContext);
    }

    private async Task ClearAllTablesAsync(DbContext dbContext)
    {
        var dataModelService = _factory.Services.GetRequiredService<IDataModelService>();
        DatabaseProvider databaseProvider = dataModelService.DatabaseProvider;

        if (databaseProvider == DatabaseProvider.SqlServer)
        {
            await dbContext.Database.ExecuteSqlRawAsync(SqlServerClearAllTablesScript);
        }
        else
        {
            foreach (IEntityType entityType in dbContext.Model.GetEntityTypes())
            {
                string? tableName = entityType.GetTableName();

                string escapedTableName = databaseProvider switch
                {
                    DatabaseProvider.PostgreSql => $"\"{tableName}\"",
                    DatabaseProvider.MySql => $"`{tableName}`",
                    _ => throw new NotSupportedException($"Unsupported database provider '{databaseProvider}'.")
                };

                await dbContext.Database.ExecuteSqlRawAsync($"DELETE FROM {escapedTableName}");
            }
        }
    }
}
