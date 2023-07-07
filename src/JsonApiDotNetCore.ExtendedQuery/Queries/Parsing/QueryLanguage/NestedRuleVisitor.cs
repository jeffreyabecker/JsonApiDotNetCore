using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public class NestedRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.NestedExprContext, ExtendedQueryExpression>
{
    public ExtendedQueryExpression Visit(IJadncFilterVisitor<ExtendedQueryExpression> visitor, JadncFiltersParser.NestedExprContext context)
    {
        return new ParentheticalExpression(visitor.Visit(context.expr()));
    }
}
