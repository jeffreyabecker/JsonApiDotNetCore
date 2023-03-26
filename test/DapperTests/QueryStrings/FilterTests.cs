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

public sealed class FilterTests : IClassFixture<DapperTestContext>
{
    private readonly DapperTestContext _testContext;
    private readonly TestFakers _fakers = new();

    public FilterTests(DapperTestContext testContext, ITestOutputHelper testOutputHelper)
    {
        testContext.SetTestOutputHelper(testOutputHelper);
        _testContext = testContext;
    }

    [Fact]
    public async Task Can_filter_equals_on_obfuscated_id_at_primary_endpoint()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        List<Tag> tags = _fakers.Tag.Generate(3);
        tags.ForEach(tag => tag.Color = _fakers.RgbColor.Generate());

        tags[0].Color!.StringId = "FF0000";
        tags[1].Color!.StringId = "00FF00";
        tags[2].Color!.StringId = "0000FF";

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await _testContext.ClearAllTablesAsync(dbContext);
            dbContext.Tags.AddRange(tags);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/tags?filter=equals(color.id,'00FF00')";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(1);
        responseDocument.Data.ManyValue[0].Type.Should().Be("tags");
        responseDocument.Data.ManyValue[0].Id.Should().Be(tags[1].StringId);

        responseDocument.Meta.Should().ContainTotal(1);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT COUNT(*)
FROM ""Tags"" AS t1
INNER JOIN ""RgbColors"" AS t2 ON t1.""Id"" = t2.""TagId""
WHERE t2.""Id"" = @p1"));

            command.Parameters.ShouldHaveCount(1);
            command.Parameters.Should().Contain("@p1", 0x00FF00);
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT t1.""Id"", t1.""Name""
FROM ""Tags"" AS t1
INNER JOIN ""RgbColors"" AS t2 ON t1.""Id"" = t2.""TagId""
WHERE t2.""Id"" = @p1
ORDER BY t1.""Id""
LIMIT @p2"));

            command.Parameters.ShouldHaveCount(2);
            command.Parameters.Should().Contain("@p1", 0x00FF00);
            command.Parameters.Should().Contain("@p2", 10);
        });
    }

    [Fact]
    public async Task Can_filter_any_on_obfuscated_id_at_primary_endpoint()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        List<Tag> tags = _fakers.Tag.Generate(3);
        tags.ForEach(tag => tag.Color = _fakers.RgbColor.Generate());

        tags[0].Color!.StringId = "FF0000";
        tags[1].Color!.StringId = "00FF00";
        tags[2].Color!.StringId = "0000FF";

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await _testContext.ClearAllTablesAsync(dbContext);
            dbContext.Tags.AddRange(tags);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/tags?filter=any(color.id,'00FF00','11EE11')";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(1);
        responseDocument.Data.ManyValue[0].Type.Should().Be("tags");
        responseDocument.Data.ManyValue[0].Id.Should().Be(tags[1].StringId);

        responseDocument.Meta.Should().ContainTotal(1);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT COUNT(*)
FROM ""Tags"" AS t1
INNER JOIN ""RgbColors"" AS t2 ON t1.""Id"" = t2.""TagId""
WHERE t2.""Id"" IN (@p1, @p2)"));

            command.Parameters.ShouldHaveCount(2);
            command.Parameters.Should().Contain("@p1", 0x00FF00);
            command.Parameters.Should().Contain("@p2", 0x11EE11);
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT t1.""Id"", t1.""Name""
FROM ""Tags"" AS t1
INNER JOIN ""RgbColors"" AS t2 ON t1.""Id"" = t2.""TagId""
WHERE t2.""Id"" IN (@p1, @p2)
ORDER BY t1.""Id""
LIMIT @p3"));

            command.Parameters.ShouldHaveCount(3);
            command.Parameters.Should().Contain("@p1", 0x00FF00);
            command.Parameters.Should().Contain("@p2", 0x11EE11);
            command.Parameters.Should().Contain("@p3", 10);
        });
    }

    [Fact]
    public async Task Can_filter_equals_null_on_relationship_at_secondary_endpoint()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        Person person = _fakers.Person.Generate();
        person.OwnedTodoItems = _fakers.TodoItem.Generate(2).ToHashSet();
        person.OwnedTodoItems.ElementAt(0).Assignee = _fakers.Person.Generate();

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            dbContext.People.Add(person);
            await dbContext.SaveChangesAsync();
        });

        string route = $"/people/{person.StringId}/ownedTodoItems?filter=equals(assignee,null)";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(1);
        responseDocument.Data.ManyValue[0].Type.Should().Be("todoItems");
        responseDocument.Data.ManyValue[0].Id.Should().Be(person.OwnedTodoItems.ElementAt(1).StringId);

        responseDocument.Meta.Should().ContainTotal(1);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT COUNT(*)
FROM ""TodoItems"" AS t1
INNER JOIN ""People"" AS t2 ON t1.""OwnerId"" = t2.""Id""
LEFT JOIN ""People"" AS t3 ON t1.""AssigneeId"" = t3.""Id""
WHERE (t2.""Id"" = @p1) AND (t3.""Id"" IS NULL)"));

            command.Parameters.ShouldHaveCount(1);
            command.Parameters.Should().Contain("@p1", person.Id);
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(
                @"SELECT t1.""Id"", t2.""Id"", t2.""CreatedAt"", t2.""Description"", t2.""DurationInHours"", t2.""LastModifiedAt"", t2.""Priority""
FROM ""People"" AS t1
INNER JOIN ""TodoItems"" AS t2 ON t1.""Id"" = t2.""OwnerId""
LEFT JOIN ""People"" AS t3 ON t2.""AssigneeId"" = t3.""Id""
WHERE (t1.""Id"" = @p1) AND (t3.""Id"" IS NULL)
ORDER BY t2.""Priority"", t2.""LastModifiedAt"" DESC
LIMIT @p2"));

            command.Parameters.ShouldHaveCount(2);
            command.Parameters.Should().Contain("@p1", person.Id);
            command.Parameters.Should().Contain("@p2", 10);
        });
    }

    [Fact]
    public async Task Can_filter_equals_null_on_attribute_at_secondary_endpoint()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        Person person = _fakers.Person.Generate();
        person.OwnedTodoItems = _fakers.TodoItem.Generate(2).ToHashSet();
        person.OwnedTodoItems.ElementAt(1).DurationInHours = null;

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            dbContext.People.Add(person);
            await dbContext.SaveChangesAsync();
        });

        string route = $"/people/{person.StringId}/ownedTodoItems?filter=equals(durationInHours,null)";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(1);
        responseDocument.Data.ManyValue[0].Type.Should().Be("todoItems");
        responseDocument.Data.ManyValue[0].Id.Should().Be(person.OwnedTodoItems.ElementAt(1).StringId);

        responseDocument.Meta.Should().ContainTotal(1);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT COUNT(*)
FROM ""TodoItems"" AS t1
INNER JOIN ""People"" AS t2 ON t1.""OwnerId"" = t2.""Id""
WHERE (t2.""Id"" = @p1) AND (t1.""DurationInHours"" IS NULL)"));

            command.Parameters.ShouldHaveCount(1);
            command.Parameters.Should().Contain("@p1", person.Id);
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(
                @"SELECT t1.""Id"", t2.""Id"", t2.""CreatedAt"", t2.""Description"", t2.""DurationInHours"", t2.""LastModifiedAt"", t2.""Priority""
FROM ""People"" AS t1
INNER JOIN ""TodoItems"" AS t2 ON t1.""Id"" = t2.""OwnerId""
WHERE (t1.""Id"" = @p1) AND (t2.""DurationInHours"" IS NULL)
ORDER BY t2.""Priority"", t2.""LastModifiedAt"" DESC
LIMIT @p2"));

            command.Parameters.ShouldHaveCount(2);
            command.Parameters.Should().Contain("@p1", person.Id);
            command.Parameters.Should().Contain("@p2", 10);
        });
    }

    [Fact]
    public async Task Can_filter_equals_on_enum_attribute_at_primary_endpoint()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        List<TodoItem> todoItems = _fakers.TodoItem.Generate(3);
        todoItems.ForEach(todoItem => todoItem.Owner = _fakers.Person.Generate());
        todoItems.ForEach(todoItem => todoItem.Priority = TodoItemPriority.Low);

        todoItems[1].Priority = TodoItemPriority.Medium;

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await _testContext.ClearAllTablesAsync(dbContext);
            dbContext.TodoItems.AddRange(todoItems);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/todoItems?filter=equals(priority,'Medium')";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(1);
        responseDocument.Data.ManyValue[0].Type.Should().Be("todoItems");
        responseDocument.Data.ManyValue[0].Id.Should().Be(todoItems[1].StringId);

        responseDocument.Meta.Should().ContainTotal(1);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT COUNT(*)
FROM ""TodoItems"" AS t1
WHERE t1.""Priority"" = @p1"));

            command.Parameters.ShouldHaveCount(1);
            command.Parameters.Should().Contain("@p1", todoItems[1].Priority);
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(
                @"SELECT t1.""Id"", t1.""CreatedAt"", t1.""Description"", t1.""DurationInHours"", t1.""LastModifiedAt"", t1.""Priority""
FROM ""TodoItems"" AS t1
WHERE t1.""Priority"" = @p1
ORDER BY t1.""Priority"", t1.""LastModifiedAt"" DESC
LIMIT @p2"));

            command.Parameters.ShouldHaveCount(2);
            command.Parameters.Should().Contain("@p1", todoItems[1].Priority);
            command.Parameters.Should().Contain("@p2", 10);
        });
    }

    [Fact]
    public async Task Can_filter_equals_on_string_attribute_at_secondary_endpoint()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        Person person = _fakers.Person.Generate();
        person.AssignedTodoItems = _fakers.TodoItem.Generate(2).ToHashSet();
        person.AssignedTodoItems.ForEach(todoItem => todoItem.Owner = _fakers.Person.Generate());

        person.AssignedTodoItems.ElementAt(1).Description = "Take exam";

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await _testContext.ClearAllTablesAsync(dbContext);
            dbContext.People.Add(person);
            await dbContext.SaveChangesAsync();
        });

        string route = $"/people/{person.StringId}/assignedTodoItems?filter=equals(description,'Take exam')";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(1);

        responseDocument.Data.ManyValue[0].Type.Should().Be("todoItems");
        responseDocument.Data.ManyValue[0].Id.Should().Be(person.AssignedTodoItems.ElementAt(1).StringId);

        responseDocument.Meta.Should().ContainTotal(1);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT COUNT(*)
FROM ""TodoItems"" AS t1
LEFT JOIN ""People"" AS t2 ON t1.""AssigneeId"" = t2.""Id""
WHERE (t2.""Id"" = @p1) AND (t1.""Description"" = @p2)"));

            command.Parameters.ShouldHaveCount(2);
            command.Parameters.Should().Contain("@p1", person.Id);
            command.Parameters.Should().Contain("@p2", person.AssignedTodoItems.ElementAt(1).Description);
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(
                @"SELECT t1.""Id"", t2.""Id"", t2.""CreatedAt"", t2.""Description"", t2.""DurationInHours"", t2.""LastModifiedAt"", t2.""Priority""
FROM ""People"" AS t1
LEFT JOIN ""TodoItems"" AS t2 ON t1.""Id"" = t2.""AssigneeId""
WHERE (t1.""Id"" = @p1) AND (t2.""Description"" = @p2)
ORDER BY t2.""Priority"", t2.""LastModifiedAt"" DESC
LIMIT @p3"));

            command.Parameters.ShouldHaveCount(3);
            command.Parameters.Should().Contain("@p1", person.Id);
            command.Parameters.Should().Contain("@p2", person.AssignedTodoItems.ElementAt(1).Description);
            command.Parameters.Should().Contain("@p3", 10);
        });
    }

    [Fact]
    public async Task Can_filter_equality_on_attributes_at_primary_endpoint()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        List<TodoItem> todoItems = _fakers.TodoItem.Generate(2);
        todoItems.ForEach(todoItem => todoItem.Owner = _fakers.Person.Generate());
        todoItems.ForEach(todoItem => todoItem.Assignee = _fakers.Person.Generate());

        todoItems[1].Assignee!.FirstName = todoItems[1].Assignee!.LastName;

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await _testContext.ClearAllTablesAsync(dbContext);
            dbContext.TodoItems.AddRange(todoItems);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/todoItems?filter=equals(assignee.lastName,assignee.firstName)";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(1);
        responseDocument.Data.ManyValue[0].Type.Should().Be("todoItems");
        responseDocument.Data.ManyValue[0].Id.Should().Be(todoItems[1].StringId);

        responseDocument.Meta.Should().ContainTotal(1);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT COUNT(*)
FROM ""TodoItems"" AS t1
LEFT JOIN ""People"" AS t2 ON t1.""AssigneeId"" = t2.""Id""
WHERE t2.""LastName"" = t2.""FirstName"""));

            command.Parameters.Should().BeEmpty();
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(
                @"SELECT t1.""Id"", t1.""CreatedAt"", t1.""Description"", t1.""DurationInHours"", t1.""LastModifiedAt"", t1.""Priority""
FROM ""TodoItems"" AS t1
LEFT JOIN ""People"" AS t2 ON t1.""AssigneeId"" = t2.""Id""
WHERE t2.""LastName"" = t2.""FirstName""
ORDER BY t1.""Priority"", t1.""LastModifiedAt"" DESC
LIMIT @p1"));

            command.Parameters.ShouldHaveCount(1);
            command.Parameters.Should().Contain("@p1", 10);
        });
    }

    [Fact]
    public async Task Can_filter_any_with_single_constant_at_secondary_endpoint()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        Person person = _fakers.Person.Generate();
        person.OwnedTodoItems = _fakers.TodoItem.Generate(2).ToHashSet();

        person.OwnedTodoItems.ElementAt(0).Priority = TodoItemPriority.Low;
        person.OwnedTodoItems.ElementAt(1).Priority = TodoItemPriority.Medium;

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            dbContext.People.Add(person);
            await dbContext.SaveChangesAsync();
        });

        string route = $"/people/{person.StringId}/ownedTodoItems?filter=any(priority,'Medium')";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(1);
        responseDocument.Data.ManyValue[0].Type.Should().Be("todoItems");
        responseDocument.Data.ManyValue[0].Id.Should().Be(person.OwnedTodoItems.ElementAt(1).StringId);

        responseDocument.Meta.Should().ContainTotal(1);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT COUNT(*)
FROM ""TodoItems"" AS t1
INNER JOIN ""People"" AS t2 ON t1.""OwnerId"" = t2.""Id""
WHERE (t2.""Id"" = @p1) AND (t1.""Priority"" = @p2)"));

            command.Parameters.ShouldHaveCount(2);
            command.Parameters.Should().Contain("@p1", person.Id);
            command.Parameters.Should().Contain("@p2", TodoItemPriority.Medium);
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(
                @"SELECT t1.""Id"", t2.""Id"", t2.""CreatedAt"", t2.""Description"", t2.""DurationInHours"", t2.""LastModifiedAt"", t2.""Priority""
FROM ""People"" AS t1
INNER JOIN ""TodoItems"" AS t2 ON t1.""Id"" = t2.""OwnerId""
WHERE (t1.""Id"" = @p1) AND (t2.""Priority"" = @p2)
ORDER BY t2.""Priority"", t2.""LastModifiedAt"" DESC
LIMIT @p3"));

            command.Parameters.ShouldHaveCount(3);
            command.Parameters.Should().Contain("@p1", person.Id);
            command.Parameters.Should().Contain("@p2", TodoItemPriority.Medium);
            command.Parameters.Should().Contain("@p3", 10);
        });
    }

    [Fact]
    public async Task Can_filter_text_at_primary_endpoint()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        List<TodoItem> todoItems = _fakers.TodoItem.Generate(3);
        todoItems.ForEach(todoItem => todoItem.Owner = _fakers.Person.Generate());

        todoItems[0].Description = "One";
        todoItems[1].Description = "Two";
        todoItems[1].Owner.FirstName = "Jack";
        todoItems[2].Description = "Three";

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await _testContext.ClearAllTablesAsync(dbContext);
            dbContext.TodoItems.AddRange(todoItems);
            await dbContext.SaveChangesAsync();
        });

        const string route =
            "/todoItems?filter=and(startsWith(description,'T'),not(any(description,'Three','Four')),equals(owner.firstName,'Jack'),contains(description,'o'))&sort=description";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(1);
        responseDocument.Data.ManyValue[0].Type.Should().Be("todoItems");
        responseDocument.Data.ManyValue[0].Id.Should().Be(todoItems[1].StringId);

        responseDocument.Meta.Should().ContainTotal(1);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT COUNT(*)
FROM ""TodoItems"" AS t1
INNER JOIN ""People"" AS t2 ON t1.""OwnerId"" = t2.""Id""
WHERE (t1.""Description"" LIKE 'T%') AND (NOT (t1.""Description"" IN (@p1, @p2))) AND (t2.""FirstName"" = @p3) AND (t1.""Description"" LIKE '%o%')"));

            command.Parameters.ShouldHaveCount(3);
            command.Parameters.Should().Contain("@p1", "Four");
            command.Parameters.Should().Contain("@p2", "Three");
            command.Parameters.Should().Contain("@p3", "Jack");
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(
                @"SELECT t1.""Id"", t1.""CreatedAt"", t1.""Description"", t1.""DurationInHours"", t1.""LastModifiedAt"", t1.""Priority""
FROM ""TodoItems"" AS t1
INNER JOIN ""People"" AS t2 ON t1.""OwnerId"" = t2.""Id""
WHERE (t1.""Description"" LIKE 'T%') AND (NOT (t1.""Description"" IN (@p1, @p2))) AND (t2.""FirstName"" = @p3) AND (t1.""Description"" LIKE '%o%')
ORDER BY t1.""Description""
LIMIT @p4"));

            command.Parameters.ShouldHaveCount(4);
            command.Parameters.Should().Contain("@p1", "Four");
            command.Parameters.Should().Contain("@p2", "Three");
            command.Parameters.Should().Contain("@p3", "Jack");
            command.Parameters.Should().Contain("@p4", 10);
        });
    }

    [Fact]
    public async Task Can_filter_numeric_range_at_primary_endpoint()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        List<TodoItem> todoItems = _fakers.TodoItem.Generate(3);
        todoItems.ForEach(todoItem => todoItem.Owner = _fakers.Person.Generate());

        todoItems[0].DurationInHours = 100;
        todoItems[1].DurationInHours = 200;
        todoItems[2].DurationInHours = 300;

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await _testContext.ClearAllTablesAsync(dbContext);
            dbContext.TodoItems.AddRange(todoItems);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/todoItems?filter=or(greaterThan(durationInHours,'250'),lessOrEqual(durationInHours,'100'))&sort=durationInHours";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(2);
        responseDocument.Data.ManyValue[0].Type.Should().Be("todoItems");
        responseDocument.Data.ManyValue[0].Id.Should().Be(todoItems[0].StringId);
        responseDocument.Data.ManyValue[1].Type.Should().Be("todoItems");
        responseDocument.Data.ManyValue[1].Id.Should().Be(todoItems[2].StringId);

        responseDocument.Meta.Should().ContainTotal(2);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT COUNT(*)
FROM ""TodoItems"" AS t1
WHERE (t1.""DurationInHours"" > @p1) OR (t1.""DurationInHours"" <= @p2)"));

            command.Parameters.ShouldHaveCount(2);
            command.Parameters.Should().Contain("@p1", 250);
            command.Parameters.Should().Contain("@p2", 100);
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(
                @"SELECT t1.""Id"", t1.""CreatedAt"", t1.""Description"", t1.""DurationInHours"", t1.""LastModifiedAt"", t1.""Priority""
FROM ""TodoItems"" AS t1
WHERE (t1.""DurationInHours"" > @p1) OR (t1.""DurationInHours"" <= @p2)
ORDER BY t1.""DurationInHours""
LIMIT @p3"));

            command.Parameters.ShouldHaveCount(3);
            command.Parameters.Should().Contain("@p1", 250);
            command.Parameters.Should().Contain("@p2", 100);
            command.Parameters.Should().Contain("@p3", 10);
        });
    }

    [Fact]
    public async Task Can_filter_count_at_primary_endpoint()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        List<TodoItem> todoItems = _fakers.TodoItem.Generate(2);
        todoItems.ForEach(todoItem => todoItem.Owner = _fakers.Person.Generate());

        todoItems[1].Owner.AssignedTodoItems = _fakers.TodoItem.Generate(2).ToHashSet();
        todoItems[1].Owner.AssignedTodoItems.ForEach(todoItem => todoItem.Owner = _fakers.Person.Generate());

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await _testContext.ClearAllTablesAsync(dbContext);
            dbContext.TodoItems.AddRange(todoItems);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/todoItems?filter=and(greaterThan(count(owner.assignedTodoItems),'1'),not(equals(owner,null)))";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(1);
        responseDocument.Data.ManyValue[0].Type.Should().Be("todoItems");
        responseDocument.Data.ManyValue[0].Id.Should().Be(todoItems[1].StringId);

        responseDocument.Meta.Should().ContainTotal(1);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT COUNT(*)
FROM ""TodoItems"" AS t1
INNER JOIN ""People"" AS t4 ON t1.""OwnerId"" = t4.""Id""
WHERE ((
    SELECT COUNT(*)
    FROM ""People"" AS t2
    INNER JOIN ""TodoItems"" AS t3 ON t2.""Id"" = t3.""AssigneeId""
    WHERE t1.""OwnerId"" = t2.""Id""
) > @p1) AND (NOT (t4.""Id"" IS NULL))"));

            command.Parameters.ShouldHaveCount(1);
            command.Parameters.Should().Contain("@p1", 1);
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(
                @"SELECT t1.""Id"", t1.""CreatedAt"", t1.""Description"", t1.""DurationInHours"", t1.""LastModifiedAt"", t1.""Priority""
FROM ""TodoItems"" AS t1
INNER JOIN ""People"" AS t4 ON t1.""OwnerId"" = t4.""Id""
WHERE ((
    SELECT COUNT(*)
    FROM ""People"" AS t2
    INNER JOIN ""TodoItems"" AS t3 ON t2.""Id"" = t3.""AssigneeId""
    WHERE t1.""OwnerId"" = t2.""Id""
) > @p1) AND (NOT (t4.""Id"" IS NULL))
ORDER BY t1.""Priority"", t1.""LastModifiedAt"" DESC
LIMIT @p2"));

            command.Parameters.ShouldHaveCount(2);
            command.Parameters.Should().Contain("@p1", 1);
            command.Parameters.Should().Contain("@p2", 10);
        });
    }

    [Fact]
    public async Task Can_filter_nested_conditional_has_at_primary_endpoint()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        List<TodoItem> todoItems = _fakers.TodoItem.Generate(2);
        todoItems.ForEach(todoItem => todoItem.Owner = _fakers.Person.Generate());

        todoItems[1].Owner.AssignedTodoItems = _fakers.TodoItem.Generate(2).ToHashSet();

        todoItems[1].Owner.AssignedTodoItems.ForEach(todoItem =>
        {
            todoItem.Description = "Homework";
            todoItem.Owner = _fakers.Person.Generate();
            todoItem.Owner.LastName = "Smith";
            todoItem.Tags = _fakers.Tag.Generate(1).ToHashSet();
        });

        todoItems[1].Owner.AssignedTodoItems.ElementAt(1).Tags.ElementAt(0).Name = "Personal";

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await _testContext.ClearAllTablesAsync(dbContext);
            dbContext.TodoItems.AddRange(todoItems);
            await dbContext.SaveChangesAsync();
        });

        const string route =
            "/todoItems?filter=has(owner.assignedTodoItems,and(has(tags,equals(name,'Personal')),equals(owner.lastName,'Smith'),equals(description,'Homework')))";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(1);
        responseDocument.Data.ManyValue[0].Type.Should().Be("todoItems");
        responseDocument.Data.ManyValue[0].Id.Should().Be(todoItems[1].StringId);

        responseDocument.Meta.Should().ContainTotal(1);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT COUNT(*)
FROM ""TodoItems"" AS t1
WHERE EXISTS (
    SELECT 1
    FROM ""People"" AS t2
    INNER JOIN ""TodoItems"" AS t3 ON t2.""Id"" = t3.""AssigneeId""
    INNER JOIN ""People"" AS t5 ON t3.""OwnerId"" = t5.""Id""
    WHERE (t1.""OwnerId"" = t2.""Id"") AND (EXISTS (
        SELECT 1
        FROM ""Tags"" AS t4
        WHERE (t3.""Id"" = t4.""TodoItemId"") AND (t4.""Name"" = @p1)
    )) AND (t5.""LastName"" = @p2) AND (t3.""Description"" = @p3)
)"));

            command.Parameters.ShouldHaveCount(3);
            command.Parameters.Should().Contain("@p1", "Personal");
            command.Parameters.Should().Contain("@p2", "Smith");
            command.Parameters.Should().Contain("@p3", "Homework");
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(
                @"SELECT t1.""Id"", t1.""CreatedAt"", t1.""Description"", t1.""DurationInHours"", t1.""LastModifiedAt"", t1.""Priority""
FROM ""TodoItems"" AS t1
WHERE EXISTS (
    SELECT 1
    FROM ""People"" AS t2
    INNER JOIN ""TodoItems"" AS t3 ON t2.""Id"" = t3.""AssigneeId""
    INNER JOIN ""People"" AS t5 ON t3.""OwnerId"" = t5.""Id""
    WHERE (t1.""OwnerId"" = t2.""Id"") AND (EXISTS (
        SELECT 1
        FROM ""Tags"" AS t4
        WHERE (t3.""Id"" = t4.""TodoItemId"") AND (t4.""Name"" = @p1)
    )) AND (t5.""LastName"" = @p2) AND (t3.""Description"" = @p3)
)
ORDER BY t1.""Priority"", t1.""LastModifiedAt"" DESC
LIMIT @p4"));

            command.Parameters.ShouldHaveCount(4);
            command.Parameters.Should().Contain("@p1", "Personal");
            command.Parameters.Should().Contain("@p2", "Smith");
            command.Parameters.Should().Contain("@p3", "Homework");
            command.Parameters.Should().Contain("@p4", 10);
        });
    }

    [Fact]
    public async Task Can_filter_conditional_has_with_null_check_at_primary_endpoint()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        List<Person> people = _fakers.Person.Generate(3);
        people.ForEach(person => person.OwnedTodoItems = _fakers.TodoItem.Generate(1).ToHashSet());

        people[0].OwnedTodoItems.ElementAt(0).Assignee = null;

        people[1].OwnedTodoItems.ElementAt(0).Assignee = _fakers.Person.Generate();

        people[2].OwnedTodoItems.ElementAt(0).Assignee = _fakers.Person.Generate();
        people[2].OwnedTodoItems.ElementAt(0).Assignee!.FirstName = null;

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await _testContext.ClearAllTablesAsync(dbContext);
            dbContext.People.AddRange(people);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/people?filter=has(ownedTodoItems,equals(assignee.firstName,null))";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(1);
        responseDocument.Data.ManyValue[0].Type.Should().Be("people");
        responseDocument.Data.ManyValue[0].Id.Should().Be(people[2].StringId);

        responseDocument.Meta.Should().ContainTotal(1);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT COUNT(*)
FROM ""People"" AS t1
WHERE EXISTS (
    SELECT 1
    FROM ""TodoItems"" AS t2
    INNER JOIN ""People"" AS t3 ON t2.""AssigneeId"" = t3.""Id""
    WHERE (t1.""Id"" = t2.""OwnerId"") AND (t3.""FirstName"" IS NULL)
)"));

            command.Parameters.Should().BeEmpty();
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT t1.""Id"", t1.""FirstName"", t1.""LastName""
FROM ""People"" AS t1
WHERE EXISTS (
    SELECT 1
    FROM ""TodoItems"" AS t2
    INNER JOIN ""People"" AS t3 ON t2.""AssigneeId"" = t3.""Id""
    WHERE (t1.""Id"" = t2.""OwnerId"") AND (t3.""FirstName"" IS NULL)
)
ORDER BY t1.""Id""
LIMIT @p1"));

            command.Parameters.ShouldHaveCount(1);
            command.Parameters.Should().Contain("@p1", 10);
        });
    }

    [Fact]
    public async Task Can_filter_using_logical_operators_at_primary_endpoint()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        List<TodoItem> todoItems = _fakers.TodoItem.Generate(5);
        todoItems.ForEach(todoItem => todoItem.Owner = _fakers.Person.Generate());

        todoItems[0].Description = "0";
        todoItems[0].Priority = TodoItemPriority.High;
        todoItems[0].DurationInHours = 1;

        todoItems[1].Description = "1";
        todoItems[1].Priority = TodoItemPriority.Low;
        todoItems[1].DurationInHours = 0;

        todoItems[2].Description = "1";
        todoItems[2].Priority = TodoItemPriority.Low;
        todoItems[2].DurationInHours = 1;

        todoItems[3].Description = "1";
        todoItems[3].Priority = TodoItemPriority.High;
        todoItems[3].DurationInHours = 0;

        todoItems[4].Description = "1";
        todoItems[4].Priority = TodoItemPriority.High;
        todoItems[4].DurationInHours = 1;

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await _testContext.ClearAllTablesAsync(dbContext);
            dbContext.TodoItems.AddRange(todoItems);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/todoItems?filter=and(equals(description,'1'),or(equals(priority,'High'),equals(durationInHours,'1')))";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(3);
        responseDocument.Data.ManyValue.Should().ContainSingle(resource => resource.Id == todoItems[2].StringId);
        responseDocument.Data.ManyValue.Should().ContainSingle(resource => resource.Id == todoItems[3].StringId);
        responseDocument.Data.ManyValue.Should().ContainSingle(resource => resource.Id == todoItems[4].StringId);

        responseDocument.Meta.Should().ContainTotal(3);

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(@"SELECT COUNT(*)
FROM ""TodoItems"" AS t1
WHERE (t1.""Description"" = @p1) AND ((t1.""Priority"" = @p2) OR (t1.""DurationInHours"" = @p3))"));

            command.Parameters.ShouldHaveCount(3);
            command.Parameters.Should().Contain("@p1", "1");
            command.Parameters.Should().Contain("@p2", TodoItemPriority.High);
            command.Parameters.Should().Contain("@p3", 1);
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_testContext.AdaptSql(
                @"SELECT t1.""Id"", t1.""CreatedAt"", t1.""Description"", t1.""DurationInHours"", t1.""LastModifiedAt"", t1.""Priority""
FROM ""TodoItems"" AS t1
WHERE (t1.""Description"" = @p1) AND ((t1.""Priority"" = @p2) OR (t1.""DurationInHours"" = @p3))
ORDER BY t1.""Priority"", t1.""LastModifiedAt"" DESC
LIMIT @p4"));

            command.Parameters.ShouldHaveCount(4);
            command.Parameters.Should().Contain("@p1", "1");
            command.Parameters.Should().Contain("@p2", TodoItemPriority.High);
            command.Parameters.Should().Contain("@p3", 1);
            command.Parameters.Should().Contain("@p4", 10);
        });
    }

    [Fact]
    public async Task Cannot_filter_on_unmapped_attribute()
    {
        // Arrange
        var store = _testContext.Factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        Person person = _fakers.Person.Generate();

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            dbContext.People.Add(person);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/people?filter=equals(displayName,'John Doe')";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.BadRequest);

        responseDocument.Errors.ShouldHaveCount(1);

        ErrorObject error = responseDocument.Errors[0];
        error.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.Title.Should().Be("Sorting or filtering on the requested attribute is unavailable.");
        error.Detail.Should().Be("Sorting or filtering on attribute 'displayName' is unavailable.");
        error.Source.Should().BeNull();
    }
}
