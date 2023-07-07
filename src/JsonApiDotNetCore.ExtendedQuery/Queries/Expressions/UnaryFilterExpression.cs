
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;

public class UnaryFilterExpression : FilterExpression
{
    public UnaryFilterExpression(UnaryFilterOperator @operator, QueryExpression operand)
    {
        Operator = @operator;
        Operand = operand;
    }

    public UnaryFilterOperator Operator { get; }
    public QueryExpression Operand { get; }

    public override TResult Accept<TArgument, TResult>(QueryExpressionVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.DefaultVisit(this, argument);
    public override string ToFullString()
    {
        return Operator.Prefix? $"{Operator} {Operand}" : $"{Operand} {Operator}";
    }
}
