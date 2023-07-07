using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage.Visitors;
public class RootRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.ExprContext, QueryExpression>
{
    public QueryExpression Visit(IJadncFilterVisitor<QueryExpression> visitor, JadncFiltersParser.ExprContext context)
    {
        context.
    }
}
