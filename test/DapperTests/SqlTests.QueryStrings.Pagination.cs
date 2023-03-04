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
    [Fact]
    public async Task Can_paginate_in_primary_resources()
    {
        // Arrange
        var store = _factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        List<TodoItem> todoItems = _fakers.TodoItem.Generate(5);
        todoItems.ForEach(todoItem => todoItem.Owner = _fakers.Person.Generate());

        await RunOnDatabaseAsync(async dbContext =>
        {
            await dbContext.ClearTablesAsync<RgbColor, Tag, TodoItem>();
            dbContext.TodoItems.AddRange(todoItems);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/todoItems?page[size]=3&page[number]=2&sort=id";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(2);
        responseDocument.Data.ManyValue[0].Id.Should().Be(todoItems[3].StringId);
        responseDocument.Data.ManyValue[1].Id.Should().Be(todoItems[4].StringId);

        responseDocument.Meta.Should().ContainTotal(5);

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
ORDER BY t1.""Id""
LIMIT @p1 OFFSET @p2");

            command.Parameters.ShouldHaveCount(2);
            command.Parameters.Should().Contain("@p1", 3);
            command.Parameters.Should().Contain("@p2", 3);
        });
    }

    [Fact(Skip = "TODO")]
    public async Task Silently_ignores_nested_pagination()
    {
        // Arrange
        var store = _factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        List<TodoItem> todoItems = _fakers.TodoItem.Generate(5);
        todoItems.ForEach(todoItem => todoItem.Owner = _fakers.Person.Generate());
        todoItems.ForEach(todoItem => todoItem.Tags = _fakers.Tag.Generate(5).ToHashSet());

        await RunOnDatabaseAsync(async dbContext =>
        {
            await dbContext.ClearTablesAsync<RgbColor, Tag, TodoItem>();
            dbContext.TodoItems.AddRange(todoItems);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/todoItems?include=tags&page[size]=2&sort=id";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(2);
        responseDocument.Data.ManyValue[0].Id.Should().Be(todoItems[0].StringId);
        responseDocument.Data.ManyValue[1].Id.Should().Be(todoItems[1].StringId);

        responseDocument.Meta.Should().ContainTotal(5);

        responseDocument.Included.ShouldHaveCount(10);

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
ORDER BY t1.""Id""
LIMIT @p1 OFFSET @p2");

            command.Parameters.ShouldHaveCount(2);
            command.Parameters.Should().Contain("@p1", 3);
            command.Parameters.Should().Contain("@p2", 3);
        });
    }
}
