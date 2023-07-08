using System.Text;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public abstract class ExtendedQueryExpression : QueryExpression
{
    public override TResult Accept<TArgument, TResult>(QueryExpressionVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.DefaultVisit(this, argument);

    public abstract void Serialize(StringBuilder sb);
    public override string ToString()
    {
        return ToFullString();
    }
    public override string ToFullString()
    {
        var sb = new StringBuilder();
        Serialize(sb);
        return sb.ToString();
    }
}
