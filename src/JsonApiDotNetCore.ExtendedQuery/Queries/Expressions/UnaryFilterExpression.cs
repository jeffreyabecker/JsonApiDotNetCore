using System.Text;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;

public class UnaryFilterExpression : ExtendedQueryExpression
{
    public UnaryFilterExpression(UnaryFilterOperator @operator, ExtendedQueryExpression operand)
    {
        Operator = @operator;
        Operand = operand;
    }

    public UnaryFilterOperator Operator { get; }
    public ExtendedQueryExpression Operand { get; }

    public override void Serialize(StringBuilder sb)
    {
        if (Operator.Prefix)
        {
            sb.Append(Operator);
        }
        Operand.Serialize(sb);
        if (!Operator.Prefix)
        {
            sb.Append(Operator);
        }
    }

    public override string ToFullString()
    {
        return Operator.Prefix? $"{Operator} {Operand?.ToFullString()}" : $"{Operand?.ToFullString()} {Operator}";
    }
}
