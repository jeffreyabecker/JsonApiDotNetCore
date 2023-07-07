using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public class NestedRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.NestedExprContext, QueryExpression>
{
    public QueryExpression Visit(IJadncFilterVisitor<QueryExpression> visitor, JadncFiltersParser.NestedExprContext context)
    {
		return visitor.Visit(context.expr())
    }
}
