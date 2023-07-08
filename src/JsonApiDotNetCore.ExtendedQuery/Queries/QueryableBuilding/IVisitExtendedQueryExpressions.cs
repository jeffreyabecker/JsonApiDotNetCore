using System.Linq.Expressions;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.Queries.QueryableBuilding;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.QueryableBuilding;
public interface IVisitExtendedQueryExpressions
{
    Expression VisitExtendedQueryExpression(ExtendedQueryExpression expression, QueryClauseBuilderContext context);
    Type ResolveFixedType(ExtendedQueryExpression expression, QueryClauseBuilderContext context);
    Type ResolveCommonType(ExtendedQueryExpression left, ExtendedQueryExpression right, QueryClauseBuilderContext context);
}
