using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Resources;

namespace JsonApiDotNetCore.Queries;

/// <summary>
/// Provides in-memory storage for the evaluated inclusion tree within a request. This tree is produced from query string and resource definition
/// callbacks. The cache enables the serialization layer to take changes from <see cref="IResourceDefinition{TResource,TId}.OnApplyIncludes" /> into
/// account.
/// </summary>
public interface IEvaluatedIncludeCache<TInclude> where TInclude : IQueryLayerInclude
{
    /// <summary>
    /// Stores the evaluated inclusion tree for later usage.
    /// </summary>
    void Set(TInclude include);

    /// <summary>
    /// Gets the evaluated inclusion tree that was stored earlier.
    /// </summary>
    TInclude? Get();
}
/// <inheritdoc />
public interface IEvaluatedIncludeCache : IEvaluatedIncludeCache<IncludeExpression>
{

}
