﻿
using JsonApiDotNetCore.Builders;
using JsonApiDotNetCore.Internal;
using JsonApiDotNetCore.Internal.Generics;
using JsonApiDotNetCore.Models;
using JsonApiDotNetCore.Services;
using JsonApiDotNetCoreExample.Models;
using JsonApiDotNetCoreExample.Resources;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;


namespace UnitTests.ResourceHooks
{

    public class ScenarioTests : ResourceHooksTestBase
    {
        public ScenarioTests()
        {
            // Build() exposes the static ResourceGraphBuilder.Instance member, which 
            // is consumed by ResourceDefinition class.
            new ResourceGraphBuilder()
                .AddResource<TodoItem>()
                .AddResource<Person>()
                .Build();
        }

        [Fact]
        public void Entity_Has_Multiple_Relations_To_Same_Type()
        {
            // arrange
            var todoDiscovery = SetDiscoverableHooks<TodoItem>();
            var personDiscovery = SetDiscoverableHooks<Person>();

            (var contextMock, var hookExecutor, var todoResourceMock,
                var ownerResourceMock) = CreateTestObjects(todoDiscovery, personDiscovery);
            var person = new Person();
            var todo = new TodoItem
            {
                Owner = person,
                Assignee = person
            };
            var todoList = new List<TodoItem>() { todo };
            person.AssignedTodoItems = todoList;
            
            // act
            hookExecutor.AfterCreate(todoList, It.IsAny<ResourceAction>());
            // assert
            todoResourceMock.Verify(rd => rd.AfterCreate(todoList, It.IsAny<ResourceAction>()), Times.Once());
            ownerResourceMock.Verify(rd => rd.AfterUpdate(It.IsAny<IEnumerable<IIdentifiable>>(), It.IsAny<ResourceAction>()), Times.Once());

            todoResourceMock.As<IResourceHookContainer<IIdentifiable>>().Verify(rd => rd.ShouldExecuteHook(It.IsAny<ResourceHook>()), Times.AtLeastOnce());
            todoResourceMock.VerifyNoOtherCalls();
            ownerResourceMock.Verify(rd => rd.ShouldExecuteHook(It.IsAny<ResourceHook>()), Times.AtLeastOnce());
            ownerResourceMock.VerifyNoOtherCalls();
        }

        //[Fact]
        //public void AfterCreate_Without_Parent_Hook_Implemented()
        //{
        //    // arrange
        //    var todoDiscovery = SetDiscoverableHooks<TodoItem>(new ResourceHook[0]);
        //    var personDiscovery = SetDiscoverableHooks<Person>();

        //    (var contextMock, var hookExecutor, var todoResourceMock,
        //        var ownerResourceMock) = CreateTestObjects(todoDiscovery, personDiscovery);
        //    var todoInput = new List<TodoItem>() { new TodoItem
        //        {
        //            Owner = new Person()
        //        }
        //    };
        //    // act
        //    hookExecutor.AfterCreate(todoList, It.IsAny<ResourceAction>());
        //    // assert
        //    ownerResourceMock.Verify(rd => rd.AfterUpdate(It.IsAny<IEnumerable<IIdentifiable>>(), It.IsAny<ResourceAction>()), Times.Once());
        //    ownerResourceMock.Verify(rd => rd.ShouldExecuteHook(It.IsAny<ResourceHook>()), Times.AtLeastOnce());
        //    ownerResourceMock.VerifyNoOtherCalls();
        //    todoResourceMock.As<IResourceHookContainer<IIdentifiable>>().Verify(rd => rd.ShouldExecuteHook(It.IsAny<ResourceHook>()), Times.AtLeastOnce());
        //    todoResourceMock.VerifyNoOtherCalls();

        //}

        //[Fact]
        //public void AfterCreate_Without_Child_Hook_Implemented()
        //{
        //    // arrange
        //    var todoDiscovery = SetDiscoverableHooks<TodoItem>();
        //    var personDiscovery = SetDiscoverableHooks<Person>(new ResourceHook[0]);

        //    (var contextMock, var hookExecutor, var todoResourceMock,
        //        var ownerResourceMock) = CreateTestObjects(todoDiscovery, personDiscovery);
        //    var todoInput = new List<TodoItem>() { new TodoItem
        //        {
        //            Owner = new Person()
        //        }
        //    };
        //    // act
        //    hookExecutor.AfterCreate(todoList, It.IsAny<ResourceAction>());
        //    // assert
        //    todoResourceMock.Verify(rd => rd.AfterCreate(todoList, It.IsAny<ResourceAction>()), Times.Once());
        //    todoResourceMock.As<IResourceHookContainer<IIdentifiable>>().Verify(rd => rd.ShouldExecuteHook(It.IsAny<ResourceHook>()), Times.AtLeastOnce());
        //    todoResourceMock.VerifyNoOtherCalls();
        //    ownerResourceMock.Verify(rd => rd.ShouldExecuteHook(It.IsAny<ResourceHook>()), Times.AtLeastOnce());
        //    ownerResourceMock.VerifyNoOtherCalls();
        //}

        //[Fact]
        //public void AfterCreate_Without_Any_Hook_Implemented()
        //{
        //    // arrange
        //    var todoDiscovery = SetDiscoverableHooks<TodoItem>(new ResourceHook[0]);
        //    var personDiscovery = SetDiscoverableHooks<Person>(new ResourceHook[0]);

        //    (var contextMock, var hookExecutor, var todoResourceMock,
        //        var ownerResourceMock) = CreateTestObjects(todoDiscovery, personDiscovery);
        //    var todoInput = new List<TodoItem>() { new TodoItem
        //        {
        //            Owner = new Person()
        //        }
        //    };
        //    // act
        //    hookExecutor.AfterCreate(todoList, It.IsAny<ResourceAction>());
        //    // assert
        //    todoResourceMock.As<IResourceHookContainer<IIdentifiable>>().Verify(rd => rd.ShouldExecuteHook(It.IsAny<ResourceHook>()), Times.AtLeastOnce());
        //    todoResourceMock.VerifyNoOtherCalls();
        //    ownerResourceMock.Verify(rd => rd.ShouldExecuteHook(It.IsAny<ResourceHook>()), Times.AtLeastOnce());
        //    ownerResourceMock.VerifyNoOtherCalls();
        //}
    }
}

