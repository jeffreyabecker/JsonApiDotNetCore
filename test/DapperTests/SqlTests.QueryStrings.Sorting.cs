using System.Net;
using DapperExample.Models;
using DapperExample.Repositories;
using FluentAssertions;
using JsonApiDotNetCore.Serialization.Objects;
using Microsoft.Extensions.DependencyInjection;
using TestBuildingBlocks;
using Xunit;

namespace DapperTests;

public sealed partial class SqlTests
{
    // TODO: Fail for sort on nested count (does not work, requires push down into sub-select).

    [Fact]
    public async Task Can_sort_on_attributes_in_primary_resources()
    {
        // Arrange
        var store = _factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        List<TodoItem> todoItems = _fakers.TodoItem.Generate(3);
        todoItems.ForEach(todoItem => todoItem.Owner = _fakers.Person.Generate());

        todoItems[0].Description = "B";
        todoItems[1].Description = "A";
        todoItems[2].Description = "C";

        await RunOnDatabaseAsync(async dbContext =>
        {
            await dbContext.ClearTablesAsync<RgbColor, Tag, TodoItem>();
            dbContext.TodoItems.AddRange(todoItems);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/todoItems?sort=-description,durationInHours,id";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(3);
        responseDocument.Data.ManyValue[0].Id.Should().Be(todoItems[2].StringId);
        responseDocument.Data.ManyValue[1].Id.Should().Be(todoItems[0].StringId);
        responseDocument.Data.ManyValue[2].Id.Should().Be(todoItems[1].StringId);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(@"SELECT COUNT(*)
FROM ""TodoItems"" AS t1");

            command.Parameters.Should().BeEmpty();
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(
                @"SELECT t1.""Id"", t1.""CreatedAt"", t1.""Description"", t1.""DurationInHours"", t1.""LastModifiedAt"", t1.""Priority""
FROM ""TodoItems"" AS t1
ORDER BY t1.""Description"" DESC, t1.""DurationInHours"", t1.""Id""
LIMIT @p1");

            command.Parameters.ShouldHaveCount(1);
            command.Parameters.Should().Contain("@p1", 10);
        });
    }

    [Fact]
    public async Task Can_sort_on_count_in_primary_resources()
    {
        // Arrange
        var store = _factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        List<TodoItem> todoItems = _fakers.TodoItem.Generate(3);
        todoItems.ForEach(todoItem => todoItem.Owner = _fakers.Person.Generate());

        todoItems[0].Tags = _fakers.Tag.Generate(2).ToHashSet();
        todoItems[1].Tags = _fakers.Tag.Generate(1).ToHashSet();
        todoItems[2].Tags = _fakers.Tag.Generate(3).ToHashSet();

        await RunOnDatabaseAsync(async dbContext =>
        {
            await dbContext.ClearTablesAsync<RgbColor, Tag, TodoItem>();
            dbContext.TodoItems.AddRange(todoItems);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/todoItems?sort=-count(tags),id";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(3);
        responseDocument.Data.ManyValue[0].Id.Should().Be(todoItems[2].StringId);
        responseDocument.Data.ManyValue[1].Id.Should().Be(todoItems[0].StringId);
        responseDocument.Data.ManyValue[2].Id.Should().Be(todoItems[1].StringId);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(@"SELECT COUNT(*)
FROM ""TodoItems"" AS t1");

            command.Parameters.Should().BeEmpty();
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(
                @"SELECT t1.""Id"", t1.""CreatedAt"", t1.""Description"", t1.""DurationInHours"", t1.""LastModifiedAt"", t1.""Priority""
FROM ""TodoItems"" AS t1
ORDER BY (
    SELECT COUNT(*)
    FROM ""TodoItems"" AS t2
    LEFT JOIN ""Tags"" AS t3 ON t2.""Id"" = t3.""TodoItemId""
    WHERE t1.""Id"" = t2.""Id""
) DESC, t1.""Id""
LIMIT @p1");

            command.Parameters.ShouldHaveCount(1);
            command.Parameters.Should().Contain("@p1", 10);
        });
    }

    [Fact]
    public async Task Can_sort_on_included_resources()
    {
        // Arrange
        var store = _factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        Person person = _fakers.Person.Generate();
        person.OwnedTodoItems = _fakers.TodoItem.Generate(3).ToHashSet();

        person.OwnedTodoItems.ElementAt(0).DurationInHours = 40;
        person.OwnedTodoItems.ElementAt(1).DurationInHours = 100;
        person.OwnedTodoItems.ElementAt(2).DurationInHours = 250;

        person.OwnedTodoItems.ElementAt(1).Tags = _fakers.Tag.Generate(2).ToHashSet();

        person.OwnedTodoItems.ElementAt(1).Tags.ElementAt(0).Name = "B";
        person.OwnedTodoItems.ElementAt(1).Tags.ElementAt(1).Name = "A";

        await RunOnDatabaseAsync(async dbContext =>
        {
            dbContext.People.AddRange(person);
            await dbContext.SaveChangesAsync();
        });

        string route = $"/people/{person.StringId}/ownedTodoItems?include=tags&sort=-durationInHours&sort[tags]=name";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(3);
        responseDocument.Data.ManyValue[0].Id.Should().Be(person.OwnedTodoItems.ElementAt(2).StringId);
        responseDocument.Data.ManyValue[1].Id.Should().Be(person.OwnedTodoItems.ElementAt(1).StringId);
        responseDocument.Data.ManyValue[2].Id.Should().Be(person.OwnedTodoItems.ElementAt(0).StringId);

        responseDocument.Included.ShouldHaveCount(2);
        responseDocument.Included[0].Id.Should().Be(person.OwnedTodoItems.ElementAt(1).Tags.ElementAt(1).StringId);
        responseDocument.Included[1].Id.Should().Be(person.OwnedTodoItems.ElementAt(1).Tags.ElementAt(0).StringId);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(@"SELECT COUNT(*)
FROM ""TodoItems"" AS t1
INNER JOIN ""People"" AS t2 ON t1.""OwnerId"" = t2.""Id""
WHERE t2.""Id"" = @p1");

            command.Parameters.ShouldHaveCount(1);
            command.Parameters.Should().Contain("@p1", person.Id);
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(
                @"SELECT t3.""Id"", t3.Id0 AS Id, t3.""CreatedAt"", t3.""Description"", t3.""DurationInHours"", t3.""LastModifiedAt"", t3.""Priority"", t4.""Id"", t4.""Name""
FROM (
    SELECT t1.""Id"", t1.""AccountId"", t1.""FirstName"", t1.""LastName"", t2.""Id"" AS Id0, t2.""AssigneeId"", t2.""CreatedAt"", t2.""Description"", t2.""DurationInHours"", t2.""LastModifiedAt"", t2.""OwnerId"", t2.""Priority""
    FROM ""People"" AS t1
    INNER JOIN ""TodoItems"" AS t2 ON t1.""Id"" = t2.""OwnerId""
    WHERE t1.""Id"" = @p1
    ORDER BY t2.""DurationInHours"" DESC
    LIMIT @p2
) AS t3
LEFT JOIN ""Tags"" AS t4 ON t3.Id0 = t4.""TodoItemId""
ORDER BY t3.""DurationInHours"" DESC, t4.""Name""");

            command.Parameters.ShouldHaveCount(2);
            command.Parameters.Should().Contain("@p1", person.Id);
            command.Parameters.Should().Contain("@p2", 10);
        });
    }
}
