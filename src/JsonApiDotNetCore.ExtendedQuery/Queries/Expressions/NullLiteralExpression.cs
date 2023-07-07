namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class NullLiteralExpression : ExtendedQueryExpression
{
    public static NullLiteralExpression Instance = new NullLiteralExpression();
    protected NullLiteralExpression() { }

    public override string ToFullString() => "null";
}
