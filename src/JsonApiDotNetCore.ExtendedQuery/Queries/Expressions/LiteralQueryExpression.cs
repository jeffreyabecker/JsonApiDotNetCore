
using System.Text;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public abstract class LiteralQueryExpression : ExtendedQueryExpression
{
    public abstract object? GetRawValue();
    public class BooleanLiteralExpression : LiteralQueryExpression
    {
        public bool Value { get; }

        public BooleanLiteralExpression(bool value)
        {
            Value = value;
        }
        public override string ToString()
        {
            return ToFullString();
        }
        public override string ToFullString()
        {
            return Value.ToString().ToLowerInvariant();
        }

        public override object GetRawValue() => Value;

        public override void Serialize(StringBuilder sb)
        {
            sb.Append(Value.ToString().ToLowerInvariant());
        }
    }
    public class NumericLiteralExpression : LiteralQueryExpression
    {
        public NumericLiteralExpression(string parsedValue)
        {
            Value = parsedValue.IndexOf(".") != -1 ? decimal.Parse(parsedValue) : long.Parse(parsedValue);
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

        public override object GetRawValue() => Value;

        public override void Serialize(StringBuilder sb)
        {
            sb.Append(Value);
        }
    }
    public class StringLiteralExpression : LiteralQueryExpression
    {
        public StringLiteralExpression(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public override object GetRawValue() => Value;

        public override void Serialize(StringBuilder sb)
        {
            sb.Append(Value.Replace("'", "''"));
        }

    }
    public class NullLiteralExpression : LiteralQueryExpression
    {
        public static NullLiteralExpression Instance = new NullLiteralExpression();
        protected NullLiteralExpression() { }

        public override object GetRawValue() => null;

        public override void Serialize(StringBuilder sb)
        {
            sb.Append("null");
        }
    }


}
