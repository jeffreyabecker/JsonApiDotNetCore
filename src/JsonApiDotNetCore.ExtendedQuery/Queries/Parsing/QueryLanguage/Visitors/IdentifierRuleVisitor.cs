using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
using JsonApiDotNetCore.QueryStrings.FieldChains;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage.Visitors;

public class IdentifierRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.IdentifierExprContext, ExtendedQueryExpression>
{
    private ResourceType _resourceType;

    public IdentifierRuleVisitor(ResourceType resourceType)
    {
        _resourceType = resourceType;
    }

    public ExtendedQueryExpression Visit(IJadncFilterVisitor<ExtendedQueryExpression> visitor, JadncFiltersParser.IdentifierExprContext ruleContext)
    {
        return ruleContext.ParseFieldChain(BuiltInPatterns.SingleField)
    }
}
