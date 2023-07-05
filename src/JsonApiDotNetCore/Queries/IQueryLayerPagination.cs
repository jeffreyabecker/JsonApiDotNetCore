using JsonApiDotNetCore.Configuration;

namespace JsonApiDotNetCore.Queries;

/// <summary>
/// Tracks values used for top-level pagination, which is a combined effort from options, query string parsing, resource definition callbacks and
/// fetching the total number of rows.
/// </summary>
public interface IQueryLayerPagination
{
    /// <summary>
    /// The value 1, unless overridden from query string or resource definition. Should not be higher than <see cref="IJsonApiOptions.MaximumPageNumber" />.
    /// </summary>
    PageNumber PageNumber { get; }

    /// <summary>
    /// The default page size from options, unless overridden from query string or resource definition. Should not be higher than
    /// <see cref="IJsonApiOptions.MaximumPageSize" />. Can be <c>null</c>, which means pagination is disabled.
    /// </summary>
    PageSize? PageSize { get; }
}
