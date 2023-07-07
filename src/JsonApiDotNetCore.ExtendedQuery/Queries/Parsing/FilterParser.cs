using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Queries.Parsing;
using JsonApiDotNetCore.Resources;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing;
public class FilterParser : IFilterParser
{
    private readonly IResourceFactory _resourceFactory;
    private readonly Stack<ResourceType> _resourceTypeStack = new();
    public FilterParser(IResourceFactory resourceFactory)
    {
        ArgumentGuard.NotNull(resourceFactory);

        _resourceFactory = resourceFactory;
    }

    public FilterExpression Parse(string source, ResourceType resourceType)
    {

        throw new NotImplementedException();
    }
}
