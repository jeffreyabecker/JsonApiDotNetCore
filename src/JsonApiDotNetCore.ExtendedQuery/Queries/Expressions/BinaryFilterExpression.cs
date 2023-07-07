namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class BinaryFilterExpression : ExtendedQueryExpression
{
    public BinaryFilterExpression(string @operator, ExtendedQueryExpression lhs, ExtendedQueryExpression rhs): this(new BinaryFilterOperator(@operator), lhs, rhs) { }
    public BinaryFilterExpression(BinaryFilterOperator @operator, ExtendedQueryExpression lhs, ExtendedQueryExpression rhs)
    {
        Operator = @operator;
        Lhs = lhs;
        Rhs = rhs;
    }

    public BinaryFilterOperator Operator { get; }
    public ExtendedQueryExpression Lhs { get; }
    public ExtendedQueryExpression Rhs { get; }

    

    public override string ToFullString()
    {
        return $"{Lhs?.ToFullString()} {Operator} {Rhs?.ToFullString()}";
    }
}
