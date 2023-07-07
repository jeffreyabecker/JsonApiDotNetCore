using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage.Visitors;
public class FunctionRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.FunctionExprContext, ExtendedQueryExpression>
{
    public ExtendedQueryExpression Visit(IJadncFilterVisitor<ExtendedQueryExpression> visitor, JadncFiltersParser.FunctionExprContext context)
    {
        return new FunctionCallExpression(context.IDENTIFIER_PART().GetText(), new ExpressionListExpression(context.expr().Select(expr => visitor.Visit(expr)), false));
    }
}
