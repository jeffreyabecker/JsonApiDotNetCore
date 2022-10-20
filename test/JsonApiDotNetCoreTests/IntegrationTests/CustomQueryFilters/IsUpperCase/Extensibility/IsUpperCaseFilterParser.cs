using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Queries.Internal.Parsing;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;

namespace JsonApiDotNetCoreTests.IntegrationTests.CustomQueryFilters.IsUpperCase.Extensibility;

internal sealed class IsUpperCaseFilterParser : FilterParser
{
    public IsUpperCaseFilterParser(IResourceFactory resourceFactory, Action<ResourceFieldAttribute, ResourceType, string>? validateSingleFieldCallback)
        : base(resourceFactory, validateSingleFieldCallback)
    {
    }

    protected override FilterExpression ParseFilter()
    {
        if (TokenStack.TryPeek(out Token? nextToken) && nextToken.Kind == TokenKind.Text && nextToken.Value == ExtraKeywords.IsUpperCase)
        {
            return ParseIsUpperCase();
        }

        return base.ParseFilter();
    }

    private IsUpperCaseExpression ParseIsUpperCase()
    {
        EatText(ExtraKeywords.IsUpperCase);
        EatSingleCharacterToken(TokenKind.OpenParen);

        ResourceFieldChainExpression targetAttribute = ParseFieldChain(FieldChainRequirements.EndsInAttribute, "Attribute name expected.");
        Type attributeType = targetAttribute.Fields[^1].Property.PropertyType;

        if (attributeType != typeof(string))
        {
            throw new QueryParseException("Attribute of type 'string' expected.");
        }

        EatSingleCharacterToken(TokenKind.CloseParen);

        return new IsUpperCaseExpression(targetAttribute);
    }
}
