using System.Collections.Immutable;
using Antlr4.Runtime.Tree;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Queries.Parsing;
using JsonApiDotNetCore.QueryStrings.FieldChains;
using JsonApiDotNetCore.Resources.Annotations;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public static class ParserRuleContextExtensions
{
    public static BinaryFilterExpression CreateBinaryFilterExpression<TContext>(this TContext context, IJadncFilterVisitor<ExtendedQueryExpression> visitor) where TContext : JadncFiltersParser.ExprContext
    {
        var lhs = visitor.Visit(context.GetChild(0));
        var op = ((ITerminalNode)context.GetChild(1)).GetText();
        var rhs = visitor.Visit(context.GetChild(2));
        return new BinaryFilterExpression(op, lhs, rhs);
    }
    public static string GetFullName(this JadncFiltersParser.IdentifierContext ctx) => String.Join(".", ctx.IDENTIFIER_PART().Select(node => node.GetText()));
    private static PatternMatchResult MatchAny(FieldChainPattern[] patterns, string fieldChain, ResourceType resourceType, FieldChainPatternMatchOptions options)
    {
        if(patterns == null || patterns.Length == 0)
        {
            throw new ArgumentNullException(nameof(patterns));
        }
        PatternMatchResult? result = null;
        foreach( var pattern in patterns)
        {
            result = pattern.Match(fieldChain, resourceType, options);
            if (result.IsSuccess)
            {
                return result;
            }
        }
        return result;
    }
    /// <summary>
    /// Parses a dot-separated path of field names into a chain of resource fields, while matching it against the specified pattern.
    /// </summary>
    public static ResourceFieldChainExpression ParseFieldChain(this JadncFiltersParser.IdentifierContext ctx, FieldChainPattern[] patterns, FieldChainPatternMatchOptions options, ResourceType resourceType,
        string? alternativeErrorMessage)
    {
        ArgumentGuard.NotNullNorEmpty(patterns);        
        ArgumentGuard.NotNull(resourceType);

        var result = MatchAny(patterns, ctx.GetFullName(), resourceType, options);

        if (!result.IsSuccess)
        {
            var patternNames = String.Join(", ", patterns.Select(pattern => pattern.GetDescription()));
            string message = result.IsFieldChainError
                ? result.FailureMessage
                : $"Field chain on resource type '{resourceType}' failed to match the patterns: {patternNames}. {result.FailureMessage}";

            throw new QueryParseException(message, ctx.Start.StartIndex + result.FailurePosition);
        }

        int chainPosition = 0;

        foreach (ResourceFieldAttribute field in result.FieldChain)
        {
            ValidateField(field, ctx.Start.StartIndex + chainPosition);

            chainPosition += field.PublicName.Length + 1;
        }

        return new ResourceFieldChainExpression(result.FieldChain.ToImmutableArray());
    }
    private static void ValidateField(ResourceFieldAttribute field, int position)
    {
        if (field.IsFilterBlocked())
        {
            string kind = field is AttrAttribute ? "attribute" : "relationship";
            throw new QueryParseException($"Filtering on {kind} '{field.PublicName}' is not allowed.", position);
        }
    }
}
