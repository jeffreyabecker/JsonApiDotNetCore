using System.Text;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class BinaryFilterExpression : ExtendedQueryExpression
{
    public BinaryFilterExpression(string @operator, ExtendedQueryExpression lhs, ExtendedQueryExpression rhs): this(new BinaryFilterOperator(@operator), lhs, rhs) { }
    public BinaryFilterExpression(BinaryFilterOperator @operator, ExtendedQueryExpression lhs, ExtendedQueryExpression rhs)
    {
        Operator = @operator;
        Left = lhs;
        Right = rhs;
    }

    public BinaryFilterOperator Operator { get; }
    public ExtendedQueryExpression Left { get; }
    public ExtendedQueryExpression Right { get; }


    public override void Serialize(StringBuilder sb)
    {
        Left.Serialize(sb);
        sb.Append(' ').Append(Operator).Append(' ');
        Right.Serialize(sb);
    }
}
