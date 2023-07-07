using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public class IfRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.IfExprContext, ExtendedQueryExpression>
{
    public ExtendedQueryExpression Visit(IJadncFilterVisitor<ExtendedQueryExpression> visitor, JadncFiltersParser.IfExprContext context)
    {
        //7 parts (IF) (expr) (THEN) (expr) (ELSE) (expr) (END)
        var condition = visitor.Visit(context.GetChild(1));
        var trueResult = visitor.Visit(context.GetChild(3));
        var falseResult = visitor.Visit(context.GetChild(5));
        return new ConditionalFilterExpression(condition, trueResult, falseResult);
    }
}
