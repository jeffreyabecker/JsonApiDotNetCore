using System.Linq.Expressions;
using JetBrains.Annotations;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Queries.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace JsonApiDotNetCore.Queries.QueryableBuilding;

/// <summary>
/// Immutable contextual state for *ClauseBuilder types.
/// </summary>
[PublicAPI]
public class QueryClauseBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection>
    where TQueryLayer : class, IQueryLayer<TInclude, TFilter, TSort, TPagination, TSelection>
    where TInclude : IQueryLayerInclude
    where TFilter : IQueryLayerFilter
    where TSort : IQueryLayerSort
    where TPagination : IQueryLayerPagination
    where TSelection : IQueryLayerSelection
{
    /// <summary>
    /// The source expression to append to.
    /// </summary>
    public Expression Source { get; }

    /// <summary>
    /// The resource type for <see cref="Source" />.
    /// </summary>
    public ResourceType ResourceType { get; }

    /// <summary>
    /// The extension type to generate calls on, typically <see cref="Queryable" /> or <see cref="Enumerable" />.
    /// </summary>
    public Type ExtensionType { get; }

    /// <summary>
    /// The Entity Framework Core entity model.
    /// </summary>
    public IModel EntityModel { get; }

    /// <summary>
    /// Used to produce unique names for lambda parameters.
    /// </summary>
    public LambdaScopeFactory LambdaScopeFactory { get; }

    /// <summary>
    /// The lambda expression currently in scope.
    /// </summary>
    public LambdaScope LambdaScope { get; }

    /// <summary>
    /// The outer driver for building query clauses.
    /// </summary>
    public IQueryableBuilder<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> QueryableBuilder { get; }

    /// <summary>
    /// Enables to pass custom state that you'd like to transfer between calls.
    /// </summary>
    public object? State { get; }

    public QueryClauseBuilderContext(Expression source, ResourceType resourceType, Type extensionType, IModel entityModel,
        LambdaScopeFactory lambdaScopeFactory, LambdaScope lambdaScope, IQueryableBuilder<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> queryableBuilder, object? state)
    {
        ArgumentGuard.NotNull(source);
        ArgumentGuard.NotNull(resourceType);
        ArgumentGuard.NotNull(extensionType);
        ArgumentGuard.NotNull(entityModel);
        ArgumentGuard.NotNull(lambdaScopeFactory);
        ArgumentGuard.NotNull(lambdaScope);
        ArgumentGuard.NotNull(queryableBuilder);

        Source = source;
        ResourceType = resourceType;
        LambdaScope = lambdaScope;
        EntityModel = entityModel;
        ExtensionType = extensionType;
        LambdaScopeFactory = lambdaScopeFactory;
        QueryableBuilder = queryableBuilder;
        State = state;
    }

    public QueryClauseBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> WithSource(Expression source)
    {
        ArgumentGuard.NotNull(source);

        return new QueryClauseBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection>(source, ResourceType, ExtensionType, EntityModel, LambdaScopeFactory, LambdaScope, QueryableBuilder, State);
    }

    public QueryClauseBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> WithLambdaScope(LambdaScope lambdaScope)
    {
        ArgumentGuard.NotNull(lambdaScope);

        return new QueryClauseBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection>(Source, ResourceType, ExtensionType, EntityModel, LambdaScopeFactory, lambdaScope, QueryableBuilder, State);
    }
}
