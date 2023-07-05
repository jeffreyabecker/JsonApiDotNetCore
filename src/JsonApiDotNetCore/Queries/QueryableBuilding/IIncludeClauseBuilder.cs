using System.Linq.Expressions;
using JsonApiDotNetCore.Queries.Expressions;
using Microsoft.EntityFrameworkCore;

namespace JsonApiDotNetCore.Queries.QueryableBuilding;

/// <summary>
/// Transforms <see cref="IncludeExpression" /> into <see cref="EntityFrameworkQueryableExtensions.Include{TEntity, TProperty}" /> calls.
/// </summary>
/// <remarks>
/// Types that implement this interface are stateless by design. Existing instances are reused recursively (perhaps this one not today, but that may
/// change), so don't store mutable state in private fields when implementing this interface or deriving from the built-in implementations. To pass
/// custom state, use the <see cref="QueryClauseBuilderContext.State" /> property. The only private field allowed is a stack where you push/pop state, so
/// it works recursively.
/// </remarks>
public interface IIncludeClauseBuilder<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection>
    where TQueryLayer : class, IQueryLayer<TInclude, TFilter, TSort, TPagination, TSelection>
    where TInclude : IQueryLayerInclude
    where TFilter : IQueryLayerFilter
    where TSort : IQueryLayerSort
    where TPagination : IQueryLayerPagination
    where TSelection : IQueryLayerSelection
{
    Expression ApplyInclude(TInclude include, QueryClauseBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> context);
}

/// <inheritdoc />
public interface IIncludeClauseBuilder : IIncludeClauseBuilder<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection>
{
}
