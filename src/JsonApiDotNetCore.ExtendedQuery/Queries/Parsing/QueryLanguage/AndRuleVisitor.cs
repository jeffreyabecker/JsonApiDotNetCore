using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public class AndRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.AndExprContext, ExtendedQueryExpression>
{
    public ExtendedQueryExpression Visit(IJadncFilterVisitor<ExtendedQueryExpression> visitor, JadncFiltersParser.AndExprContext context) => context.CreateBinaryFilterExpression(visitor);
}
