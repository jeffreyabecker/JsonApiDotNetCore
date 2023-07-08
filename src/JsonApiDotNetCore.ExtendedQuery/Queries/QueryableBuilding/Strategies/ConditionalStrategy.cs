using System.Linq.Expressions;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.Queries.QueryableBuilding;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.QueryableBuilding.Strategies;
public class ConditionalStrategy : IWhereClauseBuilderStrategy<ConditionalFilterExpression>
{
    public Expression Visit(IVisitExtendedQueryExpressions visitor, ConditionalFilterExpression conditional, QueryClauseBuilderContext context)
    {
        var condition = visitor.VisitExtendedQueryExpression(conditional.Condition, context);
        var whenTrue = visitor.VisitExtendedQueryExpression(conditional.WhenTrue, context);
        var whenFalse = visitor.VisitExtendedQueryExpression(conditional.WhenFalse, context);
        return Expression.Condition(condition, whenTrue, whenFalse);
    }
}
