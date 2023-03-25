using JsonApiDotNetCore;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Middleware;
using JsonApiDotNetCore.Queries;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Resources.Annotations;

namespace DapperExample.TranslationToSql.Transformations;

/// <summary>
/// Removes any additional <see cref="QueryLayer.Pagination" />s from the specified <see cref="QueryLayer" />.
/// </summary>
internal sealed class QueryLayerPaginationConverter
{
    private readonly IJsonApiRequest _request;

    public QueryLayerPaginationConverter(IJsonApiRequest request)
    {
        ArgumentGuard.NotNull(request);

        _request = request;
    }

    public void EnsureAtMostOnePagination(QueryLayer topLayer)
    {
        // Producing SQL for multiple levels of pagination is pretty complicated, therefore not implemented.
        // Instead we silently remove all pagination, except for the most useful one (if applicable).

        if (_request.PrimaryId != null || _request.Kind is EndpointKind.Secondary or EndpointKind.Relationship)
        {
            QueryLayer? singleQueryLayer = FindSingleNestedQueryLayer(topLayer);
            PaginationExpression? existingPagination = singleQueryLayer?.Pagination;

            RecursiveClearPagination(topLayer);

            if (singleQueryLayer != null)
            {
                singleQueryLayer.Pagination = existingPagination;
            }
        }
        else
        {
            PaginationExpression? existingPagination = topLayer.Pagination;

            RecursiveClearPagination(topLayer);

            topLayer.Pagination = existingPagination;
        }
    }

    private static QueryLayer? FindSingleNestedQueryLayer(QueryLayer topLayer)
    {
        List<QueryLayer> candidates = new();

        if (topLayer.Selection != null)
        {
            foreach (FieldSelectors selectors in topLayer.Selection.GetResourceTypes()
                .Select(resourceType => topLayer.Selection.GetOrCreateSelectors(resourceType)))
            {
                IEnumerable<QueryLayer> nextLayers = GetNestedQueryLayers(selectors);
                candidates.AddRange(nextLayers);
            }
        }

        return candidates.Count == 1 ? candidates[0] : null;
    }

    private void RecursiveClearPagination(QueryLayer queryLayer)
    {
        queryLayer.Pagination = null;

        if (queryLayer.Selection != null)
        {
            foreach (ResourceType resourceType in queryLayer.Selection.GetResourceTypes())
            {
                FieldSelectors selectors = queryLayer.Selection.GetOrCreateSelectors(resourceType);
                RecursiveClearPagination(selectors);
            }
        }
    }

    private void RecursiveClearPagination(FieldSelectors selectors)
    {
        foreach (QueryLayer nextLayer in GetNestedQueryLayers(selectors))
        {
            RecursiveClearPagination(nextLayer);
        }
    }

    private static IEnumerable<QueryLayer> GetNestedQueryLayers(FieldSelectors fieldSelectors)
    {
        foreach ((ResourceFieldAttribute field, QueryLayer? nextLayer) in fieldSelectors)
        {
            if (field is RelationshipAttribute && nextLayer != null)
            {
                yield return nextLayer;
            }
        }
    }
}
