using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Resources.Annotations;

namespace JsonApiDotNetCore.Queries.Internal.Parsing;

public interface IQueryExpressionParserFactory
{
    /// <summary>
    /// Extensibility point to create a (type derived from) <see cref="QueryStringParameterScopeParser" /> instance.
    /// </summary>
    QueryStringParameterScopeParser CreateQueryStringParameterScopeParser(FieldChainRequirements chainRequirements,
        Action<ResourceFieldAttribute, ResourceType, string>? validateSingleFieldCallback);

    /// <summary>
    /// Extensibility point to create a (type derived from) <see cref="FilterParser" /> instance.
    /// </summary>
    FilterParser CreateFilterParser(Action<ResourceFieldAttribute, ResourceType, string>? validateSingleFieldCallback);

    /// <summary>
    /// Extensibility point to create a (type derived from) <see cref="IncludeParser" /> instance.
    /// </summary>
    IncludeParser CreateIncludeParser();

    /// <summary>
    /// Extensibility point to create a (type derived from) <see cref="PaginationParser" /> instance.
    /// </summary>
    PaginationParser CreatePaginationParser(Action<ResourceFieldAttribute, ResourceType, string>? validateSingleFieldCallback);

    /// <summary>
    /// Extensibility point to create a (type derived from) <see cref="SortParser" /> instance.
    /// </summary>
    SortParser CreateSortParser(Action<ResourceFieldAttribute, ResourceType, string>? validateSingleFieldCallback);

    /// <summary>
    /// Extensibility point to create a (type derived from) <see cref="SparseFieldSetParser" /> instance.
    /// </summary>
    SparseFieldSetParser CreateSparseFieldSetParser(Action<ResourceFieldAttribute, ResourceType, string>? validateSingleFieldCallback);

    /// <summary>
    /// Extensibility point to create a (type derived from) <see cref="SparseFieldTypeParser" /> instance.
    /// </summary>
    SparseFieldTypeParser CreateSparseFieldTypeParser();
}
