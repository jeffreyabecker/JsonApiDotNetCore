using System.Linq.Expressions;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.Queries.QueryableBuilding;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.QueryableBuilding.Strategies;
public class UnaryExpressionStrategy : IWhereClauseBuilderStrategy<UnaryFilterExpression>
{
    public Expression Visit(IVisitExtendedQueryExpressions visitor, UnaryFilterExpression unaryExpr, QueryClauseBuilderContext context)
    {
        var operand = visitor.VisitExtendedQueryExpression(unaryExpr.Operand, context);

        if (unaryExpr.Operator == UnaryFilterOperator.Not)
        {
            return Expression.Not(operand);
        }
        if (unaryExpr.Operator == UnaryFilterOperator.IsNotNull)
        {
            return Expression.NotEqual(operand, Expression.Constant(null));
        }
        if (unaryExpr.Operator == UnaryFilterOperator.IsNull)
        {
            return Expression.Equal(operand, Expression.Constant(null));
        }
        throw new NotImplementedException($"I dont know how to convert a {unaryExpr.Operator} expression");
    }
}
