using System.Linq.Expressions;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.Queries.QueryableBuilding;

/// <summary>
/// Drives conversion from <see cref="QueryLayer" /> into system <see cref="Expression" /> trees.
/// </summary>
/// <remarks>
/// Types that implement this interface are stateless by design. Existing instances are reused recursively (perhaps this one not today, but that may
/// change), so don't store mutable state in private fields when implementing this interface or deriving from the built-in implementations. To pass
/// custom state, use the <see cref="QueryableBuilderContext.State" /> property. The only private field allowed is a stack where you push/pop state, so
/// it works recursively.
/// </remarks>
public interface IQueryableBuilder<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection>
    where TQueryLayer: class, IQueryLayer<TInclude, TFilter, TSort, TPagination, TSelection>    
    where TInclude : IQueryLayerInclude
    where TFilter : IQueryLayerFilter
    where TSort : IQueryLayerSort
    where TPagination : IQueryLayerPagination
    where TSelection : IQueryLayerSelection
{
    Expression ApplyQuery(TQueryLayer layer, QueryableBuilderContext<TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> context);
}


/// <summary>
/// Drives conversion from <see cref="QueryLayer" /> into system <see cref="Expression" /> trees.
/// </summary>
/// <remarks>
/// Types that implement this interface are stateless by design. Existing instances are reused recursively (perhaps this one not today, but that may
/// change), so don't store mutable state in private fields when implementing this interface or deriving from the built-in implementations. To pass
/// custom state, use the <see cref="QueryableBuilderContext.State" /> property. The only private field allowed is a stack where you push/pop state, so
/// it works recursively.
/// </remarks>
public interface IQueryableBuilder : IQueryableBuilder<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection>
{
   
}
