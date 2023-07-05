using System.Linq.Expressions;
using JetBrains.Annotations;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.Queries.QueryableBuilding;

/// <see cref="IQueryableBuilder" />
[PublicAPI]
public class QueryableBuilder<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> : IQueryableBuilder<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection>
    where TQueryLayer : class, IQueryLayer<TInclude, TFilter, TSort, TPagination, TSelection>
    where TInclude : IQueryLayerInclude
    where TFilter : IQueryLayerFilter
    where TSort : IQueryLayerSort
    where TPagination : IQueryLayerPagination
    where TSelection : IQueryLayerSelection
{
    private readonly IIncludeClauseBuilder<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> _includeClauseBuilder;
    private readonly IWhereClauseBuilder<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> _whereClauseBuilder;
    private readonly IOrderClauseBuilder<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> _orderClauseBuilder;
    private readonly ISkipTakeClauseBuilder<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> _skipTakeClauseBuilder;
    private readonly ISelectClauseBuilder<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> _selectClauseBuilder;

    public QueryableBuilder(
        IIncludeClauseBuilder<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> includeClauseBuilder,
        IWhereClauseBuilder<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> whereClauseBuilder,
        IOrderClauseBuilder<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> orderClauseBuilder,
        ISkipTakeClauseBuilder<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> skipTakeClauseBuilder,
        ISelectClauseBuilder<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> selectClauseBuilder)
    {
        ArgumentGuard.NotNull(includeClauseBuilder);
        ArgumentGuard.NotNull(whereClauseBuilder);
        ArgumentGuard.NotNull(orderClauseBuilder);
        ArgumentGuard.NotNull(skipTakeClauseBuilder);
        ArgumentGuard.NotNull(selectClauseBuilder);

        _includeClauseBuilder = includeClauseBuilder;
        _whereClauseBuilder = whereClauseBuilder;
        _orderClauseBuilder = orderClauseBuilder;
        _skipTakeClauseBuilder = skipTakeClauseBuilder;
        _selectClauseBuilder = selectClauseBuilder;
    }

    public virtual Expression ApplyQuery(TQueryLayer layer, QueryableBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> context)
    {
        ArgumentGuard.NotNull(layer);
        ArgumentGuard.NotNull(context);

        Expression expression = context.Source;

        if (layer.Include != null)
        {
            expression = ApplyInclude(expression, layer.Include, layer.ResourceType, context);
        }

        if (layer.Filter != null)
        {
            expression = ApplyFilter(expression, layer.Filter, layer.ResourceType, context);
        }

        if (layer.Sort != null)
        {
            expression = ApplySort(expression, layer.Sort, layer.ResourceType, context);
        }

        if (layer.Pagination != null)
        {
            expression = ApplyPagination(expression, layer.Pagination, layer.ResourceType, context);
        }

        if (layer.Selection is { IsEmpty: false })
        {
            expression = ApplySelection(expression, layer.Selection, layer.ResourceType, context);
        }

        return expression;
    }

    protected virtual Expression ApplyInclude(Expression source, TInclude include, ResourceType resourceType, QueryableBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> context)
    {
        using LambdaScope lambdaScope = context.LambdaScopeFactory.CreateScope(context.ElementType);
        QueryClauseBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> clauseContext = context.CreateClauseContext(this, source, resourceType, lambdaScope);

        return _includeClauseBuilder.ApplyInclude(include, clauseContext);
    }

    protected virtual Expression ApplyFilter(Expression source, TFilter filter, ResourceType resourceType, QueryableBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> context)
    {
        using LambdaScope lambdaScope = context.LambdaScopeFactory.CreateScope(context.ElementType);
        QueryClauseBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> clauseContext = context.CreateClauseContext(this, source, resourceType, lambdaScope);

        return _whereClauseBuilder.ApplyWhere(filter, clauseContext);
    }

    protected virtual Expression ApplySort(Expression source, TSort sort, ResourceType resourceType, QueryableBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> context)
    {
        using LambdaScope lambdaScope = context.LambdaScopeFactory.CreateScope(context.ElementType);
        QueryClauseBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> clauseContext = context.CreateClauseContext(this, source, resourceType, lambdaScope);

        return _orderClauseBuilder.ApplyOrderBy(sort, clauseContext);
    }

    protected virtual Expression ApplyPagination(Expression source, TPagination pagination, ResourceType resourceType, QueryableBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> context)
    {
        using LambdaScope lambdaScope = context.LambdaScopeFactory.CreateScope(context.ElementType);
        QueryClauseBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> clauseContext = context.CreateClauseContext(this, source, resourceType, lambdaScope);

        return _skipTakeClauseBuilder.ApplySkipTake(pagination, clauseContext);
    }

    protected virtual Expression ApplySelection(Expression source, TSelection selection, ResourceType resourceType, QueryableBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> context)
    {
        using LambdaScope lambdaScope = context.LambdaScopeFactory.CreateScope(context.ElementType);
        QueryClauseBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> clauseContext = context.CreateClauseContext(this, source, resourceType, lambdaScope);

        return _selectClauseBuilder.ApplySelect(selection, clauseContext);
    }
}
