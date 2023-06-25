using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.QueryStrings.FieldChains;

namespace JsonApiDotNetCore.Queries.Parsing;

/// <summary>
/// Parses the JSON:API 'sort' and 'filter' query string parameter names, which contain a resource field chain that indicates the scope its query string
/// parameter value applies to.
/// </summary>
public interface IQueryStringParameterScopeParser
{
    /// <summary>
    /// Parses the specified source into a <see cref="QueryStringParameterScopeExpression" />. Throws a <see cref="QueryParseException" /> if the input is
    /// invalid.
    /// </summary>
    /// <param name="source">
    /// The source text to read from.
    /// </param>
    /// <param name="resourceType">
    /// The resource type used to lookup JSON:API fields that are referenced in <see cref="source" />.
    /// </param>
    /// <param name="pattern">
    /// The pattern that the field chain in
    /// <param name="source">
    /// must match.
    /// </param>
    /// </param>
    /// <param name="options">
    /// The match options for
    /// <param name="pattern">.</param>
    /// </param>
    QueryStringParameterScopeExpression Parse(string source, ResourceType resourceType, FieldChainPattern pattern, FieldChainPatternMatchOptions options);
}
