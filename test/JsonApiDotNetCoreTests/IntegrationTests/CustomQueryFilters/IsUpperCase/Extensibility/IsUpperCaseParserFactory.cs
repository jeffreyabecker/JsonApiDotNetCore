using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Queries.Internal.Parsing;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;

namespace JsonApiDotNetCoreTests.IntegrationTests.CustomQueryFilters.IsUpperCase.Extensibility;

internal sealed class IsUpperCaseParserFactory : QueryExpressionParserFactory
{
    private readonly IResourceFactory _resourceFactory;

    public IsUpperCaseParserFactory(IResourceGraph resourceGraph, IResourceFactory resourceFactory)
        : base(resourceGraph, resourceFactory)
    {
        _resourceFactory = resourceFactory;
    }

    public override FilterParser CreateFilterParser(Action<ResourceFieldAttribute, ResourceType, string>? validateSingleFieldCallback)
    {
        return new IsUpperCaseFilterParser(_resourceFactory, validateSingleFieldCallback);
    }
}
