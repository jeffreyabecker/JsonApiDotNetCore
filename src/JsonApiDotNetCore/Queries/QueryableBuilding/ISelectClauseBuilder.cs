using System.Linq.Expressions;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.Queries.QueryableBuilding;

/// <summary>
/// Transforms <see cref="SparseFieldSetExpression" /> into
/// <see cref="Queryable.Select{TSource, TKey}(IQueryable{TSource}, System.Linq.Expressions.Expression{System.Func{TSource,TKey}})" /> calls.
/// </summary>
/// <remarks>
/// Types that implement this interface are stateless by design. Existing instances are reused recursively (perhaps this one not today, but that may
/// change), so don't store mutable state in private fields when implementing this interface or deriving from the built-in implementations. To pass
/// custom state, use the <see cref="QueryClauseBuilderContext.State" /> property. The only private field allowed is a stack where you push/pop state, so
/// it works recursively.
/// </remarks>
public interface ISelectClauseBuilder<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection>
    where TQueryLayer : class, IQueryLayer<TInclude, TFilter, TSort, TPagination, TSelection>
    where TInclude : IQueryLayerInclude
    where TFilter : IQueryLayerFilter
    where TSort : IQueryLayerSort
    where TPagination : IQueryLayerPagination
    where TSelection : IQueryLayerSelection
{
    Expression ApplySelect(TSelection selection, QueryClauseBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> context);
}
public interface ISelectClauseBuilder : ISelectClauseBuilder<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> { }
