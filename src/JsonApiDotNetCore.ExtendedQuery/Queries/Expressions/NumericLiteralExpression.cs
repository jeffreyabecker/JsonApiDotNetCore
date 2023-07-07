using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class NumericLiteralExpression : LiteralConstantExpression
{
    public NumericLiteralExpression(string parsedValue) : base(ParseNumeric(parsedValue), parsedValue)
    {
    }

    private static object ParseNumeric(string parsedValue)
    {
        if(parsedValue.IndexOf('.') != -1)
        {
            return double.Parse(parsedValue);
        }
        return long.Parse(parsedValue);
    }
    public override TResult Accept<TArgument, TResult>(QueryExpressionVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.DefaultVisit(this, argument);
}
