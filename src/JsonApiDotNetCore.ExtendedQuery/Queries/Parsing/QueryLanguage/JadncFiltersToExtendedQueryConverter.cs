using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public class JadncFiltersToExtendedQueryConverter : IJadncFiltersVisitor<ExtendedQueryExpression>
{
    private Configuration.ResourceType _resourceType;


    public JadncFiltersToExtendedQueryConverter(Configuration.ResourceType resourceType)
    {
        _resourceType = resourceType;
    }


    public ExtendedQueryExpression Visit(IParseTree tree) => tree.Accept(this);

    public ExtendedQueryExpression VisitAddExpr([NotNull] JadncFiltersParser.AddExprContext context) => context.CreateBinaryFilterExpression(this);

    public ExtendedQueryExpression VisitAndExpr([NotNull] JadncFiltersParser.AndExprContext context) => context.CreateBinaryFilterExpression(this);

    public ExtendedQueryExpression VisitChildren(IRuleNode node) => node.GetChild(0).Accept(this);

    public ExtendedQueryExpression VisitEqualExpr([NotNull] JadncFiltersParser.EqualExprContext context) => context.CreateBinaryFilterExpression(this);

    public ExtendedQueryExpression VisitErrorNode(IErrorNode node)
    {
        throw new ExtendedQueryParseException("Hit an error node in the parser at", node.Symbol.StartIndex);
    }

    public ExtendedQueryExpression VisitFunctionExpr([NotNull] JadncFiltersParser.FunctionExprContext context)=> new FunctionCallExpression(context.IDENTIFIER_PART().GetText(), context.GetExpressionList(this));


    public ExtendedQueryExpression VisitGreaterLessExpr([NotNull] JadncFiltersParser.GreaterLessExprContext context) => context.CreateBinaryFilterExpression(this);

    public ExtendedQueryExpression VisitHasExpr([NotNull] JadncFiltersParser.HasExprContext context)
    {
        throw new ExtendedQueryParseException("Has isnt implemented yet -- need feedback on how this syntax should work", context.Start.StartIndex);
    }

    public ExtendedQueryExpression VisitIdentifier([NotNull] JadncFiltersParser.IdentifierContext context)
    {
        throw new NotImplementedException("The individual rules interpret their identifiers directly through the field chain parser");
    }

    public ExtendedQueryExpression VisitIdentifierExpr([NotNull] JadncFiltersParser.IdentifierExprContext context)
    {
        var segments = context.identifier().GetText();
        var matchResult = QueryStrings.FieldChains.BuiltInPatterns.ToOneChainEndingInAttribute.Match(segments, _resourceType, QueryStrings.FieldChains.FieldChainPatternMatchOptions.AllowDerivedTypes);
        if (matchResult.IsSuccess)
        {
            throw new ExtendedQueryParseException($"Unable to find a field-chain matching {segments} for {_resourceType.PublicName}", context.Start.StartIndex);
        }
        return new IdentifierExpression(matchResult.FieldChain);

    }

    public ExtendedQueryExpression VisitIfExpr([NotNull] JadncFiltersParser.IfExprContext context) => new ConditionalFilterExpression(Visit(context.Condition), Visit(context.WhenTrue), Visit(context.WhenFalse));

    public ExtendedQueryExpression VisitInExpr([NotNull] JadncFiltersParser.InExprContext context) => new BinaryFilterExpression(context.Operator, Visit(context.Left), context.GetExpressionList(this));

    public ExtendedQueryExpression VisitIsNullExpr([NotNull] JadncFiltersParser.IsNullExprContext context) => new UnaryFilterExpression(new UnaryFilterOperator(context.Operator, false), Visit(context.expr()));

    public ExtendedQueryExpression VisitLikeExpr([NotNull] JadncFiltersParser.LikeExprContext context) => context.CreateBinaryFilterExpression(this);

    public ExtendedQueryExpression VisitLiteralExpr([NotNull] JadncFiltersParser.LiteralExprContext context)
    {
        var numeric = context.NUMERIC_LITERAL();
        if (numeric != null)
        {
            return new LiteralQueryExpression.NumericLiteralExpression(numeric.GetText());
        }
        if (context.K_NULL() != null)
        {
            return LiteralQueryExpression.NullLiteralExpression.Instance;
        }
        if (context.K_TRUE() != null)
        {
            return new LiteralQueryExpression.BooleanLiteralExpression(true);
        }
        if (context.K_FALSE() != null)
        {
            return new LiteralQueryExpression.BooleanLiteralExpression(false);
        }
        var str = context.STRING_LITERAL();
        if (str != null)
        {
            var stringVal = str.GetText();
            stringVal = stringVal.Substring(1, stringVal.Length - 2).Replace("''", "'");
            return new LiteralQueryExpression.StringLiteralExpression(stringVal);
        }

        throw new NotImplementedException($"No matching literal value type for{context.GetText()}");
    }

    public ExtendedQueryExpression VisitMulExpr([NotNull] JadncFiltersParser.MulExprContext context) => context.CreateBinaryFilterExpression(this);

    public ExtendedQueryExpression VisitNestedExpr([NotNull] JadncFiltersParser.NestedExprContext context) => new ParentheticalExpression(Visit(context.expr()));

    public ExtendedQueryExpression VisitNotExpr([NotNull] JadncFiltersParser.NotExprContext context) => new UnaryFilterExpression(UnaryFilterOperator.Not, Visit(context.expr()));

    public ExtendedQueryExpression VisitOfTypeExpr([NotNull] JadncFiltersParser.OfTypeExprContext context)
    {
        //var identifiers = context.identifier();
        ////context.K_NOT() != null;
        //var rhs = ResolveDerivedType(identifiers[1].GetFullName(), identifiers[1].Start.StartIndex);
        throw new ExtendedQueryParseException("Is of type isnt implemented yet -- need feedback on how this syntax should work", context.Start.StartIndex);
    }

    public ExtendedQueryExpression VisitOrExpr([NotNull] JadncFiltersParser.OrExprContext context) => context.CreateBinaryFilterExpression(this);

    public ExtendedQueryExpression VisitTerminal(ITerminalNode node)
    {
        throw new NotImplementedException("Terminal node");
    }
}
