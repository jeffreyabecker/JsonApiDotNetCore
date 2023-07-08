using static Antlr4.Runtime.Atn.SemanticContext;
using System.Text;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class ConditionalFilterExpression : ExtendedQueryExpression
{
    public ConditionalFilterExpression(ExtendedQueryExpression condition, ExtendedQueryExpression whenTrue, ExtendedQueryExpression whenFalse)
    {
        Condition = condition;
        WhenTrue = whenTrue;
        WhenFalse = whenFalse;
    }

    public ExtendedQueryExpression Condition { get; }
    public ExtendedQueryExpression WhenTrue { get; }
    public ExtendedQueryExpression WhenFalse { get; }


    public override string ToFullString()
    {
        return $"if {Condition?.ToFullString()} then {WhenTrue?.ToFullString()} else {WhenFalse?.ToFullString()} end";
    }
    public override void Serialize(StringBuilder sb)
    {
        sb.Append("if ");
        Condition.Serialize(sb);
        sb.Append(" then ");
        WhenTrue.Serialize(sb);
        sb.Append(" else ");
        WhenFalse.Serialize(sb);
        sb.Append(" end");
    }
}
