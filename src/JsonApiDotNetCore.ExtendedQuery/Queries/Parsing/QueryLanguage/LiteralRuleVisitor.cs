using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public class LiteralRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.LiteralExprContext, QueryExpression>
{
    public QueryExpression Visit(IJadncFilterVisitor<QueryExpression> visitor, JadncFiltersParser.LiteralExprContext ruleContext)
    {
        var numeric = ruleContext.NUMERIC_LITERAL();
        if (numeric != null)
        {
            return new NumericLiteralExpression(numeric.GetText());
        }
        if (ruleContext.K_NULL() != null)
        {
            return NullConstantExpression.Instance;
        }
        if (ruleContext.K_TRUE() != null)
        {
            return new BooleanLiteralExpression(true);
        }
        if (ruleContext.K_FALSE() != null)
        {
            return new BooleanLiteralExpression(false);
        }
        var str = ruleContext.STRING_LITERAL();
        if(str != null)
        {
            var stringVal = str.GetText();
            stringVal = stringVal.Substring(1, stringVal.Length - 2).Replace("''","'");
            return new StringLiteralExpression(stringVal);
        }

        throw new NotImplementedException($"No matching literal value type for{ruleContext.GetText()}");
    }
}
