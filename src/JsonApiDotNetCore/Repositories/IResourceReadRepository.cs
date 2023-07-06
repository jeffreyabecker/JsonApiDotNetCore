using JetBrains.Annotations;
using JsonApiDotNetCore.Queries;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Resources;

namespace JsonApiDotNetCore.Repositories;

/// <summary>
/// Groups read operations.
/// </summary>
/// <typeparam name="TResource">
/// The resource type.
/// </typeparam>
/// <typeparam name="TId">
/// The resource identifier type.
/// </typeparam>
[PublicAPI]
public interface IResourceReadRepository<TResource, in TId, TQueryLayer, TInclude, TFilter, TSort, TPagination, TSelection>
    where TQueryLayer : class, IQueryLayer<TInclude, TFilter, TSort, TPagination, TSelection>
    where TInclude : IQueryLayerInclude
    where TFilter : IQueryLayerFilter
    where TSort : IQueryLayerSort
    where TPagination : IQueryLayerPagination
    where TSelection : IQueryLayerSelection
    where TResource : class, IIdentifiable<TId>
{
    /// <summary>
    /// Executes a read query using the specified constraints and returns the collection of matching resources.
    /// </summary>
    Task<IReadOnlyCollection<TResource>> GetAsync(TQueryLayer queryLayer, CancellationToken cancellationToken);

    /// <summary>
    /// Executes a read query using the specified filter and returns the count of matching resources.
    /// </summary>
    Task<int> CountAsync(TFilter? filter, CancellationToken cancellationToken);
}


/// <summary>
/// Groups read operations.
/// </summary>
/// <typeparam name="TResource">
/// The resource type.
/// </typeparam>
/// <typeparam name="TId">
/// The resource identifier type.
/// </typeparam>
[PublicAPI]
public interface IResourceReadRepository<TResource, in TId> : IResourceReadRepository<TResource, TId, QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection>
    where TResource : class, IIdentifiable<TId>
{

}
