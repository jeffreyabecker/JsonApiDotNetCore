using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public class GreaterLessRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.GreaterLessExprContext, QueryExpression>
{
    public QueryExpression Visit(IJadncFilterVisitor<QueryExpression> visitor, JadncFiltersParser.GreaterLessExprContext context) => context.CreateBinaryFilterExpression(visitor);
}
