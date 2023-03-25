using System.Net;
using DapperExample.Models;
using DapperExample.Repositories;
using FluentAssertions;
using JsonApiDotNetCore.Serialization.Objects;
using Microsoft.Extensions.DependencyInjection;
using TestBuildingBlocks;
using Xunit;
using Xunit.Abstractions;

namespace DapperTests.QueryStrings;

public sealed class PaginationTests : IClassFixture<DapperTestContext>
{
    private readonly DapperTestContext _testContext;
    private readonly TestFakers _fakers = new();

    public PaginationTests(DapperTestContext testContext, ITestOutputHelper testOutputHelper)
    {
        testContext.SetTestOutputHelper(testOutputHelper);
        _testContext = testContext;
    }

    [Fact]
    public async Task Can_paginate_in_primary_resources()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        List<TodoItem> todoItems = _fakers.TodoItem.Generate(5);
        todoItems.ForEach(todoItem => todoItem.Owner = _fakers.Person.Generate());

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await _testContext.ClearAllTablesAsync(dbContext);
            dbContext.TodoItems.AddRange(todoItems);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/todoItems?page[size]=3&page[number]=2&sort=id";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(2);
        responseDocument.Data.ManyValue[0].Id.Should().Be(todoItems[3].StringId);
        responseDocument.Data.ManyValue[1].Id.Should().Be(todoItems[4].StringId);

        responseDocument.Meta.Should().ContainTotal(5);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT COUNT(*)
FROM ""TodoItems"" AS t1"));

            command.Parameters.Should().BeEmpty();
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(
                @"SELECT t1.""Id"", t1.""CreatedAt"", t1.""Description"", t1.""DurationInHours"", t1.""LastModifiedAt"", t1.""Priority""
FROM ""TodoItems"" AS t1
ORDER BY t1.""Id""
LIMIT @p1 OFFSET @p2"));

            command.Parameters.ShouldHaveCount(2);
            command.Parameters.Should().Contain("@p1", 3);
            command.Parameters.Should().Contain("@p2", 3);
        });
    }

    [Fact]
    public async Task Can_paginate_in_primary_resource_with_single_include()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        Person person = _fakers.Person.Generate();
        person.OwnedTodoItems = _fakers.TodoItem.Generate(10).ToHashSet();

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await _testContext.ClearAllTablesAsync(dbContext);
            dbContext.People.Add(person);
            await dbContext.SaveChangesAsync();
        });

        string route = $"/people/{person.StringId}?include=ownedTodoItems&page[size]=ownedTodoItems:3&page[number]=ownedTodoItems:3&sort[ownedTodoItems]=id";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.SingleValue.ShouldNotBeNull();
        responseDocument.Data.SingleValue.Id.Should().Be(person.StringId);

        responseDocument.Included.ShouldHaveCount(3);
        responseDocument.Included[0].Id.Should().Be(person.OwnedTodoItems.ElementAt(6).StringId);
        responseDocument.Included[1].Id.Should().Be(person.OwnedTodoItems.ElementAt(7).StringId);
        responseDocument.Included[2].Id.Should().Be(person.OwnedTodoItems.ElementAt(8).StringId);

        responseDocument.Meta.Should().BeNull();

        store.SqlCommands.ShouldHaveCount(1);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(
                @"SELECT t1.""Id"", t1.""FirstName"", t1.""LastName"", t2.""Id"", t2.""CreatedAt"", t2.""Description"", t2.""DurationInHours"", t2.""LastModifiedAt"", t2.""Priority""
FROM ""People"" AS t1
INNER JOIN ""TodoItems"" AS t2 ON t1.""Id"" = t2.""OwnerId""
WHERE t1.""Id"" = @p1
ORDER BY t2.""Id""
LIMIT @p2 OFFSET @p3"));

            command.Parameters.ShouldHaveCount(3);
            command.Parameters.Should().Contain("@p1", person.Id);
            command.Parameters.Should().Contain("@p2", 3);
            command.Parameters.Should().Contain("@p3", 6);
        });
    }

    [Fact]
    public async Task Can_paginate_in_primary_resource_with_includes()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        Person person = _fakers.Person.Generate();
        person.OwnedTodoItems = _fakers.TodoItem.Generate(10).ToHashSet();
        person.OwnedTodoItems.ForEach(todoItem => todoItem.Tags = _fakers.Tag.Generate(5).ToHashSet());

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await _testContext.ClearAllTablesAsync(dbContext);
            dbContext.People.Add(person);
            await dbContext.SaveChangesAsync();
        });

        string route = $"/people/{person.StringId}?include=ownedTodoItems.tags&page[size]=ownedTodoItems:3,ownedTodoItems.tags:2&sort[ownedTodoItems]=id";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.SingleValue.ShouldNotBeNull();
        responseDocument.Data.SingleValue.Id.Should().Be(person.StringId);

        responseDocument.Included.ShouldHaveCount(3 + 3 * 5);

        responseDocument.Meta.Should().BeNull();

        store.SqlCommands.ShouldHaveCount(1);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(
                @"SELECT t3.""Id"", t3.""FirstName"", t3.""LastName"", t3.Id0 AS Id, t3.""CreatedAt"", t3.""Description"", t3.""DurationInHours"", t3.""LastModifiedAt"", t3.""Priority"", t4.""Id"", t4.""Name""
FROM (
    SELECT t1.""Id"", t1.""FirstName"", t1.""LastName"", t2.""Id"" AS Id0, t2.""CreatedAt"", t2.""Description"", t2.""DurationInHours"", t2.""LastModifiedAt"", t2.""Priority""
    FROM ""People"" AS t1
    INNER JOIN ""TodoItems"" AS t2 ON t1.""Id"" = t2.""OwnerId""
    WHERE t1.""Id"" = @p1
    ORDER BY t2.""Id""
    LIMIT @p2
) AS t3
LEFT JOIN ""Tags"" AS t4 ON t3.Id0 = t4.""TodoItemId""
ORDER BY t3.Id0, t4.""Id"""));

            command.Parameters.ShouldHaveCount(2);
            command.Parameters.Should().Contain("@p1", person.Id);
            command.Parameters.Should().Contain("@p2", 3);
        });
    }

    [Fact]
    public async Task Can_paginate_in_secondary_resources()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        Person person = _fakers.Person.Generate();
        person.OwnedTodoItems = _fakers.TodoItem.Generate(10).ToHashSet();

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await _testContext.ClearAllTablesAsync(dbContext);
            dbContext.People.Add(person);
            await dbContext.SaveChangesAsync();
        });

        string route = $"/people/{person.StringId}/ownedTodoItems?page[size]=3&page[number]=3&sort=id";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(3);
        responseDocument.Data.ManyValue[0].Id.Should().Be(person.OwnedTodoItems.ElementAt(6).StringId);
        responseDocument.Data.ManyValue[1].Id.Should().Be(person.OwnedTodoItems.ElementAt(7).StringId);
        responseDocument.Data.ManyValue[2].Id.Should().Be(person.OwnedTodoItems.ElementAt(8).StringId);

        responseDocument.Meta.Should().ContainTotal(10);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT COUNT(*)
FROM ""TodoItems"" AS t1
INNER JOIN ""People"" AS t2 ON t1.""OwnerId"" = t2.""Id""
WHERE t2.""Id"" = @p1"));

            command.Parameters.ShouldHaveCount(1);
            command.Parameters.Should().Contain("@p1", person.Id);
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(
                @"SELECT t1.""Id"", t2.""Id"", t2.""CreatedAt"", t2.""Description"", t2.""DurationInHours"", t2.""LastModifiedAt"", t2.""Priority""
FROM ""People"" AS t1
INNER JOIN ""TodoItems"" AS t2 ON t1.""Id"" = t2.""OwnerId""
WHERE t1.""Id"" = @p1
ORDER BY t2.""Id""
LIMIT @p2 OFFSET @p3"));

            command.Parameters.ShouldHaveCount(3);
            command.Parameters.Should().Contain("@p1", person.Id);
            command.Parameters.Should().Contain("@p2", 3);
            command.Parameters.Should().Contain("@p3", 6);
        });
    }

    [Fact]
    public async Task Can_paginate_in_primary_resources_with_includes()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        List<TodoItem> todoItems = _fakers.TodoItem.Generate(5);
        todoItems.ForEach(todoItem => todoItem.Owner = _fakers.Person.Generate());
        todoItems.ForEach(todoItem => todoItem.Tags = _fakers.Tag.Generate(5).ToHashSet());

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await _testContext.ClearAllTablesAsync(dbContext);
            dbContext.TodoItems.AddRange(todoItems);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/todoItems?include=owner,assignee,tags&page[size]=2&sort=id";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(2);
        responseDocument.Data.ManyValue[0].Id.Should().Be(todoItems[0].StringId);
        responseDocument.Data.ManyValue[1].Id.Should().Be(todoItems[1].StringId);

        responseDocument.Included.ShouldHaveCount(2 + 2 * 5);

        responseDocument.Meta.Should().ContainTotal(5);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT COUNT(*)
FROM ""TodoItems"" AS t1"));

            command.Parameters.Should().BeEmpty();
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(
                @"SELECT t4.""Id"", t4.""CreatedAt"", t4.""Description"", t4.""DurationInHours"", t4.""LastModifiedAt"", t4.""Priority"", t4.Id0 AS Id, t4.""FirstName"", t4.""LastName"", t4.Id00 AS Id, t4.FirstName0 AS FirstName, t4.LastName0 AS LastName, t5.""Id"", t5.""Name""
FROM (
    SELECT t1.""Id"", t1.""CreatedAt"", t1.""Description"", t1.""DurationInHours"", t1.""LastModifiedAt"", t1.""Priority"", t2.""Id"" AS Id0, t2.""FirstName"", t2.""LastName"", t3.""Id"" AS Id00, t3.""FirstName"" AS FirstName0, t3.""LastName"" AS LastName0
    FROM ""TodoItems"" AS t1
    LEFT JOIN ""People"" AS t2 ON t1.""AssigneeId"" = t2.""Id""
    INNER JOIN ""People"" AS t3 ON t1.""OwnerId"" = t3.""Id""
    ORDER BY t1.""Id""
    LIMIT @p1
) AS t4
LEFT JOIN ""Tags"" AS t5 ON t4.""Id"" = t5.""TodoItemId""
ORDER BY t4.""Id"", t5.""Id"""));

            command.Parameters.ShouldHaveCount(1);
            command.Parameters.Should().Contain("@p1", 2);
        });
    }
}
