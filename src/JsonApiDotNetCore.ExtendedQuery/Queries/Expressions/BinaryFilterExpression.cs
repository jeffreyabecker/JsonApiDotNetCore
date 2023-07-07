
using JsonApiDotNetCore.Queries.Expressions;
using static Antlr4.Runtime.Atn.SemanticContext;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class BinaryFilterExpression : FilterExpression
{
    public BinaryFilterExpression(string @operator, QueryExpression lhs, QueryExpression rhs): this(new BinaryFilterOperator(@operator), lhs, rhs) { }
    public BinaryFilterExpression(BinaryFilterOperator @operator, QueryExpression lhs, QueryExpression rhs)
    {
        Operator = @operator;
        Lhs = lhs;
        Rhs = rhs;
    }

    public BinaryFilterOperator Operator { get; }
    public QueryExpression Lhs { get; }
    public QueryExpression Rhs { get; }

    public override TResult Accept<TArgument, TResult>(QueryExpressionVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.DefaultVisit(this, argument);

    public override string ToFullString()
    {
        return $"{Lhs} {Operator} {Rhs}";
    }
}
