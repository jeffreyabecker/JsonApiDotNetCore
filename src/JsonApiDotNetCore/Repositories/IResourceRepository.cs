using JetBrains.Annotations;
using JsonApiDotNetCore.Queries;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Resources;

namespace JsonApiDotNetCore.Repositories;

/// <summary>
/// Represents the foundational Resource Repository layer in the JsonApiDotNetCore architecture that provides data access to an underlying store.
/// </summary>
/// <typeparam name="TResource">
/// The resource type.
/// </typeparam>
/// <typeparam name="TId">
/// The resource identifier type.
/// </typeparam>
[PublicAPI]
public interface IResourceRepository<TResource, in TId, TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection> : IResourceReadRepository<TResource, TId, TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection>, IResourceWriteRepository<TResource, TId, TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection>
    where TResource : class, IIdentifiable<TId>
    where TQueryLayer : class, IQueryLayer<TInclude, TFilter, TSort, TPagination, TSelection>
    where TInclude : IQueryLayerInclude
    where TFilter : IQueryLayerFilter
    where TSort : IQueryLayerSort
    where TPagination : IQueryLayerPagination
    where TSelection : IQueryLayerSelection
{
}


/// <summary>
/// Represents the foundational Resource Repository layer in the JsonApiDotNetCore architecture that provides data access to an underlying store.
/// </summary>
/// <typeparam name="TResource">
/// The resource type.
/// </typeparam>
/// <typeparam name="TId">
/// The resource identifier type.
/// </typeparam>
[PublicAPI]
public interface IResourceRepository<TResource, in TId> :
    IResourceReadRepository<TResource, TId, QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection>,
    IResourceWriteRepository<TResource, TId, QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection>
    where TResource : class, IIdentifiable<TId>
{
}
