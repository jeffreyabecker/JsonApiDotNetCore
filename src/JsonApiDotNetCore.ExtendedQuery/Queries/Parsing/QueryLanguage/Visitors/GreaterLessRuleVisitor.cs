using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage.Visitors;
public class GreaterLessRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.GreaterLessExprContext, ExtendedQueryExpression>
{
    public ExtendedQueryExpression Visit(IJadncFilterVisitor<ExtendedQueryExpression> visitor, JadncFiltersParser.GreaterLessExprContext context) => context.CreateBinaryFilterExpression(visitor);
}
