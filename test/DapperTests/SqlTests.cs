using System.Text.Json;
using DapperExample.Data;
using DapperExample.Models;
using DapperExample.Repositories;
using FluentAssertions.Common;
using FluentAssertions.Extensions;
using JsonApiDotNetCore.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestBuildingBlocks;
using Xunit;
using Xunit.Abstractions;

namespace DapperTests;

public sealed partial class SqlTests : IntegrationTest, IClassFixture<WebApplicationFactory<TodoItem>>
{
    private static readonly DateTimeOffset FrozenTime = 29.September(2018).At(16, 41, 56).AsUtc().ToDateTimeOffset();

    private readonly WebApplicationFactory<TodoItem> _factory;
    private readonly TestFakers _fakers = new();

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
}
