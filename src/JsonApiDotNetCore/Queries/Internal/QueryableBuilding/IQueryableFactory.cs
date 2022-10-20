using System.Linq.Expressions;
using JsonApiDotNetCore.Configuration;
using Microsoft.EntityFrameworkCore.Metadata;

namespace JsonApiDotNetCore.Queries.Internal.QueryableBuilding;

public interface IQueryableFactory
{
    /// <summary>
    /// Extensibility point to create a (type derived from) <see cref="QueryableBuilder" /> instance.
    /// </summary>
    QueryableBuilder CreateQueryableBuilder(Expression source, Type elementType, Type extensionType, LambdaParameterNameFactory nameFactory, IModel entityModel,
        LambdaScopeFactory? lambdaScopeFactory);

    /// <summary>
    /// Extensibility point to create a (type derived from) <see cref="IncludeClauseBuilder" /> instance.
    /// </summary>
    IncludeClauseBuilder CreateIncludeClauseBuilder(Expression source, LambdaScope lambdaScope, ResourceType resourceType);

    /// <summary>
    /// Extensibility point to create a (type derived from) <see cref="WhereClauseBuilder" /> instance.
    /// </summary>
    WhereClauseBuilder CreateWhereClauseBuilder(Expression source, LambdaScope lambdaScope, Type extensionType, LambdaParameterNameFactory nameFactory);

    /// <summary>
    /// Extensibility point to create a (type derived from) <see cref="OrderClauseBuilder" /> instance.
    /// </summary>
    OrderClauseBuilder CreateOrderClauseBuilder(Expression source, LambdaScope lambdaScope, Type extensionType);

    /// <summary>
    /// Extensibility point to create a (type derived from) <see cref="SkipTakeClauseBuilder" /> instance.
    /// </summary>
    SkipTakeClauseBuilder CreateSkipTakeClauseBuilder(Expression source, LambdaScope lambdaScope, Type extensionType);

    /// <summary>
    /// Extensibility point to create a (type derived from) <see cref="SelectClauseBuilder" /> instance.
    /// </summary>
    SelectClauseBuilder CreateSelectClauseBuilder(Expression source, LambdaScope lambdaScope, IModel entityModel, Type extensionType,
        LambdaParameterNameFactory nameFactory);
}
