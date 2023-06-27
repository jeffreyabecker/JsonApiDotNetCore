using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Queries.Parsing;
using JsonApiDotNetCore.QueryStrings;
using JsonApiDotNetCore.QueryStrings.FieldChains;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;

namespace JsonApiDotNetCoreTests.IntegrationTests.QueryStrings.FilterFunctions.IsUpperCase.Extensions;

internal sealed class IsUpperCaseFilterParser : FilterParser
{
    public IsUpperCaseFilterParser(IResourceFactory resourceFactory, IEnumerable<IFilterValueConverter> filterValueConverters)
        : base(resourceFactory, filterValueConverters)
    {
    }

    protected override FilterExpression ParseFilter()
    {
        if (TokenStack.TryPeek(out Token? nextToken) && nextToken is { Kind: TokenKind.Text, Value: ExtraKeywords.IsUpperCase })
        {
            return ParseIsUpperCase();
        }

        return base.ParseFilter();
    }

    private IsUpperCaseExpression ParseIsUpperCase()
    {
        EatText(ExtraKeywords.IsUpperCase);
        EatSingleCharacterToken(TokenKind.OpenParen);

        ResourceFieldChainExpression targetAttributeChain =
            ParseFieldChain(BuiltInPatterns.ToOneChainEndingInAttribute, FieldChainPatternMatchOptions.None, ResourceTypeInScope, null);

        EatSingleCharacterToken(TokenKind.CloseParen);

        return new IsUpperCaseExpression(targetAttributeChain);
    }

    protected override void ValidateField(ResourceFieldAttribute field, int position)
    {
        if (field is AttrAttribute attrAttribute && attrAttribute.Property.PropertyType != typeof(string))
        {
            throw new QueryParseException("Attribute of type 'String' expected.", position);
        }

        base.ValidateField(field, position);
    }
}
