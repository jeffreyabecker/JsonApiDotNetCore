using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage.Visitors;
public class LikeRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.LikeExprContext, ExtendedQueryExpression>
{
    public ExtendedQueryExpression Visit(IJadncFilterVisitor<ExtendedQueryExpression> visitor, JadncFiltersParser.LikeExprContext context)
    {
        var lhs = visitor.Visit(context.expr(0));
        var rhs = visitor.Visit(context.expr(1));
        return new BinaryFilterExpression(context.K_NOT() != null ? BinaryFilterOperator.Like : BinaryFilterOperator.NotLike, lhs, rhs);
    }
}
