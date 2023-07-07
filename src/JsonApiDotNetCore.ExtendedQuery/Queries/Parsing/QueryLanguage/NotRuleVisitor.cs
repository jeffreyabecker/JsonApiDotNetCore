using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public class NotRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.NotExprContext, ExtendedQueryExpression>
{
    public ExtendedQueryExpression Visit(IJadncFilterVisitor<ExtendedQueryExpression> visitor, JadncFiltersParser.NotExprContext context)
    {
        return new UnaryFilterExpression( UnaryFilterOperator.Not, visitor.Visit(context.expr()));
    }
}
