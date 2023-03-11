using System.Net;
using DapperExample.Models;
using DapperExample.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestBuildingBlocks;
using Xunit;

namespace DapperTests;

public sealed partial class SqlTests
{
    [Fact]
    public async Task Can_add_to_OneToMany_relationship()
    {
        // Arrange
        var store = _factory.Services.GetRequiredService<SqlCaptureStore>();
        store.Clear();

        Person existingPerson = _fakers.Person.Generate();
        existingPerson.OwnedTodoItems = _fakers.TodoItem.Generate(1).ToHashSet();

        List<TodoItem> existingTodoItems = _fakers.TodoItem.Generate(2);
        existingTodoItems.ForEach(todoItem => todoItem.Owner = _fakers.Person.Generate());

        await RunOnDatabaseAsync(async dbContext =>
        {
            await dbContext.ClearTablesAsync<RgbColor, Tag, TodoItem>();
            dbContext.People.Add(existingPerson);
            dbContext.TodoItems.AddRange(existingTodoItems);
            await dbContext.SaveChangesAsync();
        });

        var requestBody = new
        {
            data = new[]
            {
                new
                {
                    type = "todoItems",
                    id = existingTodoItems.ElementAt(0).StringId
                },
                new
                {
                    type = "todoItems",
                    id = existingTodoItems.ElementAt(1).StringId
                }
            }
        };

        string route = $"/people/{existingPerson.StringId}/relationships/ownedTodoItems";

        // Act
        (HttpResponseMessage httpResponse, string responseDocument) = await ExecutePostAsync<string>(route, requestBody);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        responseDocument.Should().BeEmpty();

        await RunOnDatabaseAsync(async dbContext =>
        {
            Person personInDatabase = await dbContext.People.Include(person => person.OwnedTodoItems).FirstWithIdAsync(existingPerson.Id);

            personInDatabase.OwnedTodoItems.ShouldHaveCount(3);
        });

        store.SqlCommands.ShouldHaveCount(1);

        store.SqlCommands[0].With(command =>
        {
            command.Statement.Should().Be(@"UPDATE ""TodoItems""
SET ""OwnerId"" = @p1
WHERE ""Id"" IN (@p2, @p3)");

            command.Parameters.ShouldHaveCount(3);
            command.Parameters.Should().Contain("@p1", existingPerson.Id);
            command.Parameters.Should().Contain("@p2", existingTodoItems.ElementAt(0).Id);
            command.Parameters.Should().Contain("@p3", existingTodoItems.ElementAt(1).Id);
        });
    }
}