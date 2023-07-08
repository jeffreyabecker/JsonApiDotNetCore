namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;

public class BinaryFilterOperator
{
    public static readonly BinaryFilterOperator Add = new BinaryFilterOperator("+");
    public static readonly BinaryFilterOperator Subtract = new BinaryFilterOperator("-");
    public static readonly BinaryFilterOperator Multiply = new BinaryFilterOperator("*");
    public static readonly BinaryFilterOperator Divide = new BinaryFilterOperator("/");
    public static readonly BinaryFilterOperator Modulo = new BinaryFilterOperator("%");
    public static readonly BinaryFilterOperator GreaterThan = new BinaryFilterOperator(">");
    public static readonly BinaryFilterOperator LessThan = new BinaryFilterOperator("<");
    public static readonly BinaryFilterOperator GreaterThanOrEqual = new BinaryFilterOperator(">=");
    public static readonly BinaryFilterOperator LessThanOrEqual = new BinaryFilterOperator("<=");
    public static readonly BinaryFilterOperator Equal = new BinaryFilterOperator("=");
    public static readonly BinaryFilterOperator NotEqual = new BinaryFilterOperator("<>");
    public static readonly BinaryFilterOperator And = new BinaryFilterOperator("and");
    public static readonly BinaryFilterOperator Or = new BinaryFilterOperator("or");
    public static readonly BinaryFilterOperator Like = new BinaryFilterOperator("like");
    public static readonly BinaryFilterOperator NotLike= new BinaryFilterOperator("not like");
    public static readonly BinaryFilterOperator In = new BinaryFilterOperator("in");
    public static readonly BinaryFilterOperator NotIn = new BinaryFilterOperator("not in");

    private string _operator;

    public BinaryFilterOperator(string @operator)
    {
        _operator = @operator;
    }

    public override bool Equals(object? obj)
    {
        if(obj == null) return false;
        if (obj is BinaryFilterOperator that)
        {
            return this._operator == that._operator;
        }
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(_operator);

    public override string? ToString() => _operator;
}
