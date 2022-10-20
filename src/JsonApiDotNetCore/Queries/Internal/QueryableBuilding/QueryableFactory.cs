using System.Linq.Expressions;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Resources;
using Microsoft.EntityFrameworkCore.Metadata;

namespace JsonApiDotNetCore.Queries.Internal.QueryableBuilding;

public class QueryableFactory : IQueryableFactory
{
    private readonly IResourceFactory _resourceFactory;

    public QueryableFactory(IResourceFactory resourceFactory)
    {
        _resourceFactory = resourceFactory;
    }

    public virtual QueryableBuilder CreateQueryableBuilder(Expression source, Type elementType, Type extensionType, LambdaParameterNameFactory nameFactory,
        IModel entityModel, LambdaScopeFactory? lambdaScopeFactory)
    {
        return new QueryableBuilder(source, elementType, extensionType, nameFactory, entityModel, lambdaScopeFactory, this);
    }

    public virtual IncludeClauseBuilder CreateIncludeClauseBuilder(Expression source, LambdaScope lambdaScope, ResourceType resourceType)
    {
        return new IncludeClauseBuilder(source, lambdaScope, resourceType);
    }

    public virtual WhereClauseBuilder CreateWhereClauseBuilder(Expression source, LambdaScope lambdaScope, Type extensionType,
        LambdaParameterNameFactory nameFactory)
    {
        return new WhereClauseBuilder(source, lambdaScope, extensionType, nameFactory, this);
    }

    public virtual OrderClauseBuilder CreateOrderClauseBuilder(Expression source, LambdaScope lambdaScope, Type extensionType)
    {
        return new OrderClauseBuilder(source, lambdaScope, extensionType);
    }

    public virtual SkipTakeClauseBuilder CreateSkipTakeClauseBuilder(Expression source, LambdaScope lambdaScope, Type extensionType)
    {
        return new SkipTakeClauseBuilder(source, lambdaScope, extensionType);
    }

    public virtual SelectClauseBuilder CreateSelectClauseBuilder(Expression source, LambdaScope lambdaScope, IModel entityModel, Type extensionType,
        LambdaParameterNameFactory nameFactory)
    {
        return new SelectClauseBuilder(source, lambdaScope, entityModel, extensionType, nameFactory, _resourceFactory, this);
    }
}
