using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public static BinaryFilterExpression CreateBinaryFilterExpression<TContext>(this TContext context, IJadncFilterVisitor<QueryExpression> visitor) where TContext : JadncFiltersParser.ExprContext
    {
        var lhs = visitor.Visit(context.GetChild(0));
        var op = ((ITerminalNode)context.GetChild(1)).GetText();
        var rhs = visitor.Visit(context.GetChild(2));
        return new BinaryFilterExpression(op, lhs, rhs);
    }
    public static string GetFullName(this JadncFiltersParser.IdentifierContext ctx) => String.Join(".", ctx.IDENTIFIER_PART().Select(node => node.GetText()));
    /// <summary>
    /// Parses a dot-separated path of field names into a chain of resource fields, while matching it against the specified pattern.
    /// </summary>
    public static ResourceFieldChainExpression ParseFieldChain(this JadncFiltersParser.IdentifierContext ctx, FieldChainPattern pattern, FieldChainPatternMatchOptions options, ResourceType resourceType,
        string? alternativeErrorMessage)
    {
        ArgumentGuard.NotNull(pattern);
        ArgumentGuard.NotNull(resourceType);

        PatternMatchResult result = pattern.Match(ctx.GetFullName(), resourceType, options);

        if (!result.IsSuccess)
        {
            string message = result.IsFieldChainError
                ? result.FailureMessage
                : $"Field chain on resource type '{resourceType}' failed to match the pattern: {pattern.GetDescription()}. {result.FailureMessage}";

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
