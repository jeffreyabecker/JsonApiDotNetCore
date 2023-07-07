using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
using JsonApiDotNetCore.Queries.Parsing;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage.Visitors;
public class OfTypeRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.OfTypeExprContext, ExtendedQueryExpression>
{
    private ResourceType _resourceType;

    public OfTypeRuleVisitor(ResourceType resourceType)
    {
        _resourceType = resourceType;
    }

    public ExtendedQueryExpression Visit(IJadncFilterVisitor<ExtendedQueryExpression> visitor, JadncFiltersParser.OfTypeExprContext context)
    {
        var identifiers = context.identifier();
        //context.K_NOT() != null;
        var rhs = ResolveDerivedType(identifiers[1].GetFullName(), identifiers[1].Start.StartIndex);
    }
    private ResourceType ResolveDerivedType(string derivedTypeName, int position)
    {
        ResourceType? derivedType = GetDerivedType(_resourceType, derivedTypeName);

        if (derivedType == null)
        {
            throw new QueryParseException($"Resource type '{derivedTypeName}' does not exist or does not derive from '{_resourceType.PublicName}'.", position);
        }

        return derivedType;
    }

    private static ResourceType? GetDerivedType(ResourceType baseType, string publicName)
    {
        foreach (ResourceType derivedType in baseType.DirectlyDerivedTypes)
        {
            if (derivedType.PublicName == publicName)
            {
                return derivedType;
            }

            ResourceType? nextType = GetDerivedType(derivedType, publicName);

            if (nextType != null)
            {
                return nextType;
            }
        }

        return null;
    }
}
