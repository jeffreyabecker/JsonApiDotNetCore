using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage.Visitors;
public class InRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.InExprContext, ExtendedQueryExpression>
{
    public ExtendedQueryExpression Visit(IJadncFilterVisitor<ExtendedQueryExpression> visitor, JadncFiltersParser.InExprContext context)
    {
        var expressions = context.expr().Select(expr => visitor.Visit(expr)).ToList();
        var lhs = expressions[0];
        var rhs = new ExpressionListExpression(expressions.Skip(1), false);
        var operation = context.K_NOT != null ? BinaryFilterOperator.NotIn : BinaryFilterOperator.In;
        return new BinaryFilterExpression(operation, lhs, rhs);

    }
}
