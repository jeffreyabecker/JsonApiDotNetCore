using System.Linq.Expressions;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.Queries.QueryableBuilding;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.QueryableBuilding.Strategies;
public class LiteralValueStrategy : IWhereClauseBuilderStrategy<LiteralQueryExpression>
{
    public Expression Visit(IVisitExtendedQueryExpressions visitor, LiteralQueryExpression literalExpr, QueryClauseBuilderContext context) => Expression.Constant(literalExpr.GetRawValue());
}
