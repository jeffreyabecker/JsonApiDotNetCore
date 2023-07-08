using System.Linq.Expressions;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.Queries.QueryableBuilding;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.QueryableBuilding.Strategies;

public class ParentheticalStrategy : IWhereClauseBuilderStrategy<ParentheticalExpression>
{
    public Expression Visit(IVisitExtendedQueryExpressions visitor, ParentheticalExpression expression, QueryClauseBuilderContext context) => visitor.VisitExtendedQueryExpression(expression.Inner, context);
}
