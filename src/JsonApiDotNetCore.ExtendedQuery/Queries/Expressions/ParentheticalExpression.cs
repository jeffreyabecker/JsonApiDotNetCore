using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class ParentheticalExpression : ExtendedQueryExpression
{
    public ParentheticalExpression(ExtendedQueryExpression inner)
    {
        Inner = inner;
    }

    public ExtendedQueryExpression Inner { get; }

    public override TResult Accept<TArgument, TResult>(QueryExpressionVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.DefaultVisit(this.Inner, argument);

    public override string ToFullString()
    {
        return $"({Inner?.ToFullString()})";
    }
}
