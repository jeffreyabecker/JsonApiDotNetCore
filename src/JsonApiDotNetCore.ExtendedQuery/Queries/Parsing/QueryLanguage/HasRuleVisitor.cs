using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public class HasRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.HasExprContext, ExtendedQueryExpression>
{
    private ResourceType _resourceType;

    public HasRuleVisitor(ResourceType resourceType)
    {
        _resourceType = resourceType;
    }

    public ExtendedQueryExpression Visit(IJadncFilterVisitor<ExtendedQueryExpression> visitor, JadncFiltersParser.HasExprContext context)
    {
		throw new NotImplementedException();
    }
}
