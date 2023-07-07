namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class StringLiteralExpression : ExtendedQueryExpression
{
    public StringLiteralExpression(string value) 
    {
        Value = value;
    }

    public string Value { get; }

    public override string ToFullString()
    {
        return $"'{Value.Replace("'", "''")}'";
    }
}
