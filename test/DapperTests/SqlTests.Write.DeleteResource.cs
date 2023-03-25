using System.Net;
using DapperExample.Models;
using DapperExample.Repositories;
using FluentAssertions;
using JsonApiDotNetCore.Serialization.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestBuildingBlocks;
using Xunit;

namespace DapperTests;

public sealed partial class SqlTests
{
    [Fact]
    public async Task Can_delete_resource()
    {
        // Arrange
        var store = _factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        TodoItem existingTodoItem = _fakers.TodoItem.Generate();
        existingTodoItem.Owner = _fakers.Person.Generate();
        existingTodoItem.Tags = _fakers.Tag.Generate(1).ToHashSet();
        existingTodoItem.Tags.ElementAt(0).Color = _fakers.RgbColor.Generate();

        await RunOnDatabaseAsync(async dbContext =>
        {
            await ClearAllTablesAsync(dbContext);
            dbContext.TodoItems.Add(existingTodoItem);
            await dbContext.SaveChangesAsync();
        });

        string route = $"/todoItems/{existingTodoItem.StringId}";

        // Act
        (HttpResponseMessage httpResponse, string responseDocument) = await ExecuteDeleteAsync<string>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        responseDocument.Should().BeEmpty();

        await RunOnDatabaseAsync(async dbContext =>
        {
            TodoItem? todoItemInDatabase = await dbContext.TodoItems.FirstWithIdOrDefaultAsync(existingTodoItem.Id);

            todoItemInDatabase.Should().BeNull();

            List<Tag> tags = await dbContext.Tags.Where(tag => tag.TodoItem == null).ToListAsync();

            tags.ShouldHaveCount(1);
        });

        store.SqlCommands.ShouldHaveCount(1);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_adapter.Adapt(@"DELETE FROM ""TodoItems""
WHERE ""Id"" = @p1"));

            command.Parameters.ShouldHaveCount(1);
            command.Parameters.Should().Contain("@p1", existingTodoItem.Id);
        });
    }

    [Fact]
    public async Task Cannot_delete_unknown_resource()
    {
        // Arrange
        var store = _factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        const long unknownTodoItemId = Unknown.TypedId.Int64;

        string route = $"/todoItems/{unknownTodoItemId}";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await ExecuteDeleteAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.NotFound);

        responseDocument.Errors.ShouldHaveCount(1);

        ErrorObject error = responseDocument.Errors[0];
        error.StatusCode.Should().Be(HttpStatusCode.NotFound);
        error.Title.Should().Be("The requested resource does not exist.");
        error.Detail.Should().Be($"Resource of type 'todoItems' with ID '{unknownTodoItemId}' does not exist.");
        error.Source.Should().BeNull();

        store.SqlCommands.ShouldHaveCount(2);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(_adapter.Adapt(@"DELETE FROM ""TodoItems""
WHERE ""Id"" = @p1"));

            command.Parameters.ShouldHaveCount(1);
            command.Parameters.Should().Contain("@p1", unknownTodoItemId);
        });

        store.SqlCommands[1].With(command =>
        {
            command.Statement.Should().Be(_adapter.Adapt(@"SELECT t1.""Id""
FROM ""TodoItems"" AS t1
WHERE t1.""Id"" = @p1"));

            command.Parameters.ShouldHaveCount(1);
            command.Parameters.Should().Contain("@p1", unknownTodoItemId);
        });
    }
}
