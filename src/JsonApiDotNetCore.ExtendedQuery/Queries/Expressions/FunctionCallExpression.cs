namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class FunctionCallExpression : ExtendedQueryExpression
{


    public FunctionCallExpression(string name, ExpressionListExpression arguments)
    {
        Name = name;
        Arguments = arguments;
    }

    public string Name { get; }
    public ExpressionListExpression Arguments { get; }


    public override string ToFullString()
    {
        return $"{Name} {Arguments?.ToFullString()}";
    }
}
