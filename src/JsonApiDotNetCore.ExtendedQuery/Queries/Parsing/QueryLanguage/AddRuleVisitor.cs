using Antlr4.Runtime.Tree;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public class AddRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.AddExprContext, ExtendedQueryExpression>
{
    public ExtendedQueryExpression Visit(IJadncFilterVisitor<ExtendedQueryExpression> visitor, JadncFiltersParser.AddExprContext context) => context.CreateBinaryFilterExpression(visitor);
}
