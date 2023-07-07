using Antlr4.Runtime.Tree;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public class MulRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.MulExprContext, QueryExpression>
{
    public QueryExpression Visit(IJadncFilterVisitor<QueryExpression> visitor, JadncFiltersParser.MulExprContext context) => context.CreateBinaryFilterExpression(visitor);
}
