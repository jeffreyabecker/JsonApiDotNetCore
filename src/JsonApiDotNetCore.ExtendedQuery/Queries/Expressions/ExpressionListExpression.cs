using System.Collections.Immutable;
using System.Text;


namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class ExpressionListExpression : ExtendedQueryExpression
{
    public ExpressionListExpression(IEnumerable<ExtendedQueryExpression> expressions, bool hasCommaPrefix = false): this(expressions.ToImmutableList(), hasCommaPrefix) { }
    public ExpressionListExpression(IImmutableList<ExtendedQueryExpression> expressions, bool hasCommaPrefix = false)
    {
        Expressions = expressions;
        HasCommaPrefix = hasCommaPrefix;
    }

    public IImmutableList<ExtendedQueryExpression> Expressions { get; }
    public bool HasCommaPrefix { get; }

    public override string ToFullString()
    {
        var sb= new StringBuilder();
        sb.Append("(");        
        for(int i = 0; i < Expressions.Count; i++)
        {
            if(i > 0 || HasCommaPrefix)
            {
                sb.Append(",");
            }
            sb.Append(Expressions[i].ToFullString());
        }
        sb.Append(")");
        return sb.ToString();
    }
}
