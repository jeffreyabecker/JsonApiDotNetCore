using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Diagnostics;
using JsonApiDotNetCoreExample;
using JsonApiDotNetCoreExample.Data;
using JsonApiDotNetCoreExample.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

[assembly: ExcludeFromCodeCoverage]

WebApplication app = CreateWebApplication(args);

await CreateDatabaseAsync(app.Services);

app.Run();

static WebApplication CreateWebApplication(string[] args)
{
    using ICodeTimerSession codeTimerSession = new DefaultCodeTimerSession();
    CodeTimingSessionManager.Capture(codeTimerSession);

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    ConfigureServices(builder);

    WebApplication webApplication = builder.Build();

    // Configure the HTTP request pipeline.
    ConfigurePipeline(webApplication);

    if (CodeTimingSessionManager.IsEnabled)
    {
        string timingResults = CodeTimingSessionManager.Current.GetResults();
        webApplication.Logger.LogInformation($"Measurement results for application startup:{Environment.NewLine}{timingResults}");
    }

    return webApplication;
}

static void ConfigureServices(WebApplicationBuilder builder)
{
    using IDisposable _ = CodeTimingSessionManager.Current.Measure("Configure services");

    builder.Services.TryAddSingleton<ISystemClock, SystemClock>();

    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        string? connectionString = GetConnectionString(builder.Configuration);

        options.UseNpgsql(connectionString);

#if DEBUG
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
#endif

    });

    using (CodeTimingSessionManager.Current.Measure("AddJsonApi()"))
    {
        builder.Services.AddJsonApi<AppDbContext>(options =>
        {
            options.Namespace = "api/v1";
            options.UseRelativeLinks = true;
            options.IncludeTotalResourceCount = true;
            options.SerializerOptions.WriteIndented = true;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            //options.DefaultPageSize = null;

#if DEBUG
            options.IncludeExceptionStackTraceInErrors = true;
            options.IncludeRequestBodyInErrors = true;
#endif

        }, discovery => discovery.AddCurrentAssembly());
    }
}

static string? GetConnectionString(IConfiguration configuration)
{
    string postgresPassword = Environment.GetEnvironmentVariable("PGPASSWORD") ?? "postgres";
    return configuration["Data:DefaultConnection"]?.Replace("###", postgresPassword);
}

static void ConfigurePipeline(WebApplication webApplication)
{
    using IDisposable _ = CodeTimingSessionManager.Current.Measure("Configure pipeline");

    webApplication.UseRouting();

    using (CodeTimingSessionManager.Current.Measure("UseJsonApi()"))
    {
        webApplication.UseJsonApi();
    }

    webApplication.MapControllers();
}

static async Task CreateDatabaseAsync(IServiceProvider serviceProvider)
{
    await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureDeletedAsync();
    await dbContext.Database.EnsureCreatedAsync();

    await CreateSampleDataAsync(dbContext);
}

static async Task CreateSampleDataAsync(AppDbContext dbContext)
{
    RotatingList<LoginAccount> loginAccounts = RotatingList.Create(50, index => new LoginAccount
    {
        UserName = $"UserName{index + 1}",
        LastUsedAt = DateTimeOffset.UtcNow,
        Recovery = new AccountRecovery
        {
            EmailAddress = $"Email{index + 1}",
            PhoneNumber = $"Phone{index + 1}"
        }
    });

    RotatingList<Person> people = RotatingList.Create(100, index =>
    {
        var person = new Person
        {
            FirstName = $"FirstName{index + 1}",
            LastName = $"LastName{index + 1}"
        };

        if (index % 4 != 0)
        {
            person.Account = loginAccounts.GetNext();
        }

        return person;
    });

    RotatingList<RgbColor> rgbColors = RotatingList.Create(1000, index => new RgbColor
    {
        Id = index
    });

    RotatingList<Tag> tags = RotatingList.Create(2000, index =>
    {
        var tag = new Tag
        {
            Name = $"TagName{index + 1}"
        };

        if (index % 2 == 0)
        {
            tag.Color = rgbColors.GetNext();
        }

        return tag;
    });

    RotatingList<TodoItemPriority> priorities = RotatingList.Create(3, index => (TodoItemPriority)(index + 1));

    RotatingList<TodoItem> todoItems = RotatingList.Create(500, index =>
    {
        var todoItem = new TodoItem
        {
            Description = $"TodoItem{index + 1}",
            Priority = priorities.GetNext(),
            DurationInHours = index,
            CreatedAt = DateTimeOffset.UtcNow,
            Owner = people.GetNext(),
            Tags = new HashSet<Tag>
            {
                tags.GetNext(),
                tags.GetNext(),
                tags.GetNext()
            }
        };

        if (index % 3 == 0)
        {
            todoItem.Assignee = people.GetNext();
        }

        return todoItem;
    });

    dbContext.TodoItems.AddRange(todoItems.Elements);
    await dbContext.SaveChangesAsync();
}
