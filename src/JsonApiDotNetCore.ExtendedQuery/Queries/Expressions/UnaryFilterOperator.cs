namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;

public class UnaryFilterOperator
{
    public static readonly UnaryFilterOperator Not = new UnaryFilterOperator("not", true);
    public static readonly UnaryFilterOperator IsNotNull = new UnaryFilterOperator("is not null", false);
    public static readonly UnaryFilterOperator IsNull = new UnaryFilterOperator ("is null", false);
    private string _operator;
    private readonly bool _prefix;

    public bool Prefix => _prefix;

    public UnaryFilterOperator(string @operator, bool prefix)
    {
        _operator = @operator;
        _prefix = prefix;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        if (obj is UnaryFilterOperator that)
        {
            return this._operator == that._operator && this._prefix == that._prefix;
        }
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(_operator, _prefix);

    public override string? ToString() => _operator;
}
