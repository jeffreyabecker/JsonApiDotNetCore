using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public class IfRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.IfExprContext, QueryExpression>
{
    public QueryExpression Visit(IJadncFilterVisitor<QueryExpression> visitor, JadncFiltersParser.IfExprContext context)
    {
		throw new NotImplementedException();
    }
}
