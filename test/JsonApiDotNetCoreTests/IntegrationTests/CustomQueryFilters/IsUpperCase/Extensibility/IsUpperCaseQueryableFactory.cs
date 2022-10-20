using System.Linq.Expressions;
using JsonApiDotNetCore.Queries.Internal.QueryableBuilding;
using JsonApiDotNetCore.Resources;

namespace JsonApiDotNetCoreTests.IntegrationTests.CustomQueryFilters.IsUpperCase.Extensibility;

internal sealed class IsUpperCaseQueryableFactory : QueryableFactory
{
    public IsUpperCaseQueryableFactory(IResourceFactory resourceFactory)
        : base(resourceFactory)
    {
    }

    public override WhereClauseBuilder CreateWhereClauseBuilder(Expression source, LambdaScope lambdaScope, Type extensionType,
        LambdaParameterNameFactory nameFactory)
    {
        return new IsUpperCaseWhereClauseBuilder(source, lambdaScope, extensionType, nameFactory, this);
    }
}
