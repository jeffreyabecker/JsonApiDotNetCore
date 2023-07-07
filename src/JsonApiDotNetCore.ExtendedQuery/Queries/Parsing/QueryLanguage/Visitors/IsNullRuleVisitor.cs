using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage.Visitors;
public class IsNullRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.IsNullExprContext, ExtendedQueryExpression>
{
    public ExtendedQueryExpression Visit(IJadncFilterVisitor<ExtendedQueryExpression> visitor, JadncFiltersParser.IsNullExprContext context)
    {
        return new UnaryFilterExpression(context.K_NOT != null ? UnaryFilterOperator.IsNotNull : UnaryFilterOperator.IsNull, visitor.Visit(context.expr()));
    }
}
