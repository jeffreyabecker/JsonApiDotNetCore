using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class BooleanLiteralExpression : QueryExpression
{
    public bool Value { get; }

    public BooleanLiteralExpression(bool value) 
    {
        Value = value;
    }
    public override string ToString()
    {
        return ToFullString();
    }
    public override string ToFullString()
    {
        return Value.ToString().ToLowerInvariant();
    }
    public override TResult Accept<TArgument, TResult>(QueryExpressionVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.DefaultVisit(this, argument);
}
