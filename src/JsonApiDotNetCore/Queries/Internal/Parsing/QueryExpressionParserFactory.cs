using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;

namespace JsonApiDotNetCore.Queries.Internal.Parsing;

public class QueryExpressionParserFactory : IQueryExpressionParserFactory
{
    private readonly IResourceGraph _resourceGraph;
    private readonly IResourceFactory _resourceFactory;

    public QueryExpressionParserFactory(IResourceGraph resourceGraph, IResourceFactory resourceFactory)
    {
        ArgumentGuard.NotNull(resourceGraph);
        ArgumentGuard.NotNull(resourceFactory);

        _resourceGraph = resourceGraph;
        _resourceFactory = resourceFactory;
    }

    public virtual QueryStringParameterScopeParser CreateQueryStringParameterScopeParser(FieldChainRequirements chainRequirements,
        Action<ResourceFieldAttribute, ResourceType, string>? validateSingleFieldCallback)
    {
        return new QueryStringParameterScopeParser(chainRequirements, validateSingleFieldCallback);
    }

    public virtual FilterParser CreateFilterParser(Action<ResourceFieldAttribute, ResourceType, string>? validateSingleFieldCallback)
    {
        return new FilterParser(_resourceFactory, validateSingleFieldCallback);
    }

    public virtual IncludeParser CreateIncludeParser()
    {
        return new IncludeParser();
    }

    public virtual PaginationParser CreatePaginationParser(Action<ResourceFieldAttribute, ResourceType, string>? validateSingleFieldCallback)
    {
        return new PaginationParser(validateSingleFieldCallback);
    }

    public virtual SortParser CreateSortParser(Action<ResourceFieldAttribute, ResourceType, string>? validateSingleFieldCallback)
    {
        return new SortParser(validateSingleFieldCallback);
    }

    public virtual SparseFieldSetParser CreateSparseFieldSetParser(Action<ResourceFieldAttribute, ResourceType, string>? validateSingleFieldCallback)
    {
        return new SparseFieldSetParser(validateSingleFieldCallback);
    }

    public virtual SparseFieldTypeParser CreateSparseFieldTypeParser()
    {
        return new SparseFieldTypeParser(_resourceGraph);
    }
}
