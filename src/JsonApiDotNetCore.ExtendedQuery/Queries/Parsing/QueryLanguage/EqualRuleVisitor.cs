using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public class EqualRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.EqualExprContext, ExtendedQueryExpression>
{
    public ExtendedQueryExpression Visit(IJadncFilterVisitor<ExtendedQueryExpression> visitor, JadncFiltersParser.EqualExprContext context) => context.CreateBinaryFilterExpression(visitor);
}
