namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class NumericLiteralExpression : ExtendedQueryExpression
{
    public NumericLiteralExpression(string parsedValue)
    {
        Value = parsedValue.IndexOf(".") != -1? decimal.Parse(parsedValue) : long.Parse(parsedValue);
    }
    public NumericLiteralExpression(long value)
    {
        Value = value;
    }
    public NumericLiteralExpression(decimal value)
    {
        Value = value;
    }
    public object Value { get; set; }


    public override string ToFullString()
    {
        return Value.ToString()!;
    }
}
