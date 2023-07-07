using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class BooleanLiteralExpression : LiteralConstantExpression
{
    public BooleanLiteralExpression(bool typedValue) : base(typedValue, typedValue.ToString().ToLowerInvariant())
    {
    }
    public override string ToString()
    {
        return ToFullString();
    }
    public override string ToFullString()
    {
        return TypedValue.ToString()!.ToLowerInvariant();
    }
    public override TResult Accept<TArgument, TResult>(QueryExpressionVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.DefaultVisit(this, argument);
}
