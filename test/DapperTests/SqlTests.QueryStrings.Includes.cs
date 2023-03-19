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
    public async Task Can_get_primary_resources_with_multiple_include_chains()
    {
        // Arrange
        var store = _factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        Person owner = _fakers.Person.Generate();

        List<TodoItem> todoItems = _fakers.TodoItem.Generate(2);
        todoItems.ForEach(todoItem => todoItem.Owner = owner);
        todoItems.ForEach(todoItem => todoItem.Tags = _fakers.Tag.Generate(2).ToHashSet());
        todoItems[1].Assignee = _fakers.Person.Generate();

        todoItems[0].Priority = TodoItemPriority.High;
        todoItems[1].Priority = TodoItemPriority.Low;

        await RunOnDatabaseAsync(async dbContext =>
        {
            await dbContext.ClearTablesAsync<RgbColor, Tag, TodoItem>();
            dbContext.TodoItems.AddRange(todoItems);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/todoItems?include=owner.assignedTodoItems,assignee,tags";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(2);
        responseDocument.Data.ManyValue[0].ShouldNotBeNull();
        responseDocument.Data.ManyValue[0].Type.Should().Be("todoItems");
        responseDocument.Data.ManyValue[0].Id.Should().Be(todoItems[0].StringId);

        responseDocument.Data.ManyValue[0].Relationships.With(relationships =>
        {
            relationships.ShouldContainKey("owner").With(value =>
            {
                value.ShouldNotBeNull();
                value.Data.SingleValue.ShouldNotBeNull();
                value.Data.SingleValue.Type.Should().Be("people");
                value.Data.SingleValue.Id.Should().Be(todoItems[0].Owner.StringId);
            });

            relationships.ShouldContainKey("assignee").With(value =>
            {
                value.ShouldNotBeNull();
                value.Data.SingleValue.Should().BeNull();
            });

            relationships.ShouldContainKey("tags").With(value =>
            {
                value.ShouldNotBeNull();
                value.Data.ManyValue.ShouldHaveCount(2);
                value.Data.ManyValue[0].Type.Should().Be("tags");
                value.Data.ManyValue[0].Id.Should().Be(todoItems[0].Tags.ElementAt(0).StringId);
                value.Data.ManyValue[1].Type.Should().Be("tags");
                value.Data.ManyValue[1].Id.Should().Be(todoItems[0].Tags.ElementAt(1).StringId);
            });
        });

        responseDocument.Data.ManyValue[1].ShouldNotBeNull();
        responseDocument.Data.ManyValue[1].Type.Should().Be("todoItems");
        responseDocument.Data.ManyValue[1].Id.Should().Be(todoItems[1].StringId);

        responseDocument.Data.ManyValue[1].Relationships.With(relationships =>
        {
            relationships.ShouldContainKey("owner").With(value =>
            {
                value.ShouldNotBeNull();
                value.Data.SingleValue.ShouldNotBeNull();
                value.Data.SingleValue.Type.Should().Be("people");
                value.Data.SingleValue.Id.Should().Be(todoItems[1].Owner.StringId);
            });

            relationships.ShouldContainKey("assignee").With(value =>
            {
                value.ShouldNotBeNull();
                value.Data.SingleValue.ShouldNotBeNull();
                value.Data.SingleValue.Type.Should().Be("people");
                value.Data.SingleValue.Id.Should().Be(todoItems[1].Assignee!.StringId);
            });

            relationships.ShouldContainKey("tags").With(value =>
            {
                value.ShouldNotBeNull();
                value.Data.ManyValue.ShouldHaveCount(2);
                value.Data.ManyValue[0].Type.Should().Be("tags");
                value.Data.ManyValue[0].Id.Should().Be(todoItems[1].Tags.ElementAt(0).StringId);
                value.Data.ManyValue[1].Type.Should().Be("tags");
                value.Data.ManyValue[1].Id.Should().Be(todoItems[1].Tags.ElementAt(1).StringId);
            });
        });

        responseDocument.Included.ShouldHaveCount(6);

        responseDocument.Included[0].Type.Should().Be("people");
        responseDocument.Included[0].Id.Should().Be(owner.StringId);
        responseDocument.Included[0].Attributes.ShouldContainKey("firstName").With(value => value.Should().Be(owner.FirstName));
        responseDocument.Included[0].Attributes.ShouldContainKey("lastName").With(value => value.Should().Be(owner.LastName));

        responseDocument.Included[1].Type.Should().Be("tags");
        responseDocument.Included[1].Id.Should().Be(todoItems[0].Tags.ElementAt(0).StringId);
        responseDocument.Included[1].Attributes.ShouldContainKey("name").With(value => value.Should().Be(todoItems[0].Tags.ElementAt(0).Name));

        responseDocument.Included[2].Type.Should().Be("tags");
        responseDocument.Included[2].Id.Should().Be(todoItems[0].Tags.ElementAt(1).StringId);
        responseDocument.Included[2].Attributes.ShouldContainKey("name").With(value => value.Should().Be(todoItems[0].Tags.ElementAt(1).Name));

        responseDocument.Included[3].Type.Should().Be("people");
        responseDocument.Included[3].Id.Should().Be(todoItems[1].Assignee!.StringId);
        responseDocument.Included[3].Attributes.ShouldContainKey("firstName").With(value => value.Should().Be(todoItems[1].Assignee!.FirstName));
        responseDocument.Included[3].Attributes.ShouldContainKey("lastName").With(value => value.Should().Be(todoItems[1].Assignee!.LastName));

        responseDocument.Included[4].Type.Should().Be("tags");
        responseDocument.Included[4].Id.Should().Be(todoItems[1].Tags.ElementAt(0).StringId);
        responseDocument.Included[4].Attributes.ShouldContainKey("name").With(value => value.Should().Be(todoItems[1].Tags.ElementAt(0).Name));

        responseDocument.Included[5].Type.Should().Be("tags");
        responseDocument.Included[5].Id.Should().Be(todoItems[1].Tags.ElementAt(1).StringId);
        responseDocument.Included[5].Attributes.ShouldContainKey("name").With(value => value.Should().Be(todoItems[1].Tags.ElementAt(1).Name));

        responseDocument.Meta.Should().ContainTotal(2);

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
                @"SELECT t4.""Id"", t4.""CreatedAt"", t4.""Description"", t4.""DurationInHours"", t4.""LastModifiedAt"", t4.""Priority"", t4.Id0 AS Id, t4.""FirstName"", t4.""LastName"", t4.Id00 AS Id, t4.FirstName0 AS FirstName, t4.LastName0 AS LastName, t5.""Id"", t5.""CreatedAt"", t5.""Description"", t5.""DurationInHours"", t5.""LastModifiedAt"", t5.""Priority"", t6.""Id"", t6.""Name""
FROM (
    SELECT t1.""Id"", t1.""AssigneeId"", t1.""CreatedAt"", t1.""Description"", t1.""DurationInHours"", t1.""LastModifiedAt"", t1.""OwnerId"", t1.""Priority"", t2.""Id"" AS Id0, t2.""AccountId"", t2.""FirstName"", t2.""LastName"", t3.""Id"" AS Id00, t3.""AccountId"" AS AccountId0, t3.""FirstName"" AS FirstName0, t3.""LastName"" AS LastName0
    FROM ""TodoItems"" AS t1
    LEFT JOIN ""People"" AS t2 ON t1.""AssigneeId"" = t2.""Id""
    INNER JOIN ""People"" AS t3 ON t1.""OwnerId"" = t3.""Id""
    ORDER BY t1.""Priority"", t1.""LastModifiedAt"" DESC
    LIMIT @p1
) AS t4
LEFT JOIN ""TodoItems"" AS t5 ON t4.Id00 = t5.""AssigneeId""
LEFT JOIN ""Tags"" AS t6 ON t4.""Id"" = t6.""TodoItemId""
ORDER BY t4.""Priority"", t4.""LastModifiedAt"" DESC, t5.""Priority"", t5.""LastModifiedAt"" DESC, t6.""Id""");

            command.Parameters.ShouldHaveCount(1);
            command.Parameters.Should().Contain("@p1", 10);
        });
    }

    [Fact]
    public async Task Can_get_primary_resources_with_includes()
    {
        // Arrange
        var store = _factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        List<TodoItem> todoItems = _fakers.TodoItem.Generate(25);
        todoItems.ForEach(todoItem => todoItem.Owner = _fakers.Person.Generate());
        todoItems.ForEach(todoItem => todoItem.Tags = _fakers.Tag.Generate(15).ToHashSet());
        todoItems.ForEach(todoItem => todoItem.Tags.ForEach(tag => tag.Color = _fakers.RgbColor.Generate()));

        await RunOnDatabaseAsync(async dbContext =>
        {
            await dbContext.ClearTablesAsync<RgbColor, Tag, TodoItem>();
            dbContext.TodoItems.AddRange(todoItems);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/todoItems?include=tags.color";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(10);

        responseDocument.Data.ManyValue.ForEach(resource =>
        {
            resource.ShouldNotBeNull();
            resource.Type.Should().Be("todoItems");
            resource.Attributes.ShouldOnlyContainKeys("description", "priority", "durationInHours", "createdAt", "modifiedAt");
            resource.Relationships.ShouldOnlyContainKeys("owner", "assignee", "tags");
        });

        responseDocument.Included.ShouldHaveCount(10 * 15 * 2);

        responseDocument.Meta.Should().ContainTotal(25);

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
                @"SELECT t2.""Id"", t2.""CreatedAt"", t2.""Description"", t2.""DurationInHours"", t2.""LastModifiedAt"", t2.""Priority"", t3.""Id"", t3.""Name"", t4.""Id""
FROM (
    SELECT t1.""Id"", t1.""AssigneeId"", t1.""CreatedAt"", t1.""Description"", t1.""DurationInHours"", t1.""LastModifiedAt"", t1.""OwnerId"", t1.""Priority""
    FROM ""TodoItems"" AS t1
    ORDER BY t1.""Priority"", t1.""LastModifiedAt"" DESC
    LIMIT @p1
) AS t2
LEFT JOIN ""Tags"" AS t3 ON t2.""Id"" = t3.""TodoItemId""
INNER JOIN ""RgbColors"" AS t4 ON t3.""Id"" = t4.""TagId""
ORDER BY t2.""Priority"", t2.""LastModifiedAt"" DESC, t3.""Id""");

            command.Parameters.ShouldHaveCount(1);
            command.Parameters.Should().Contain("@p1", 10);
        });
    }
}
