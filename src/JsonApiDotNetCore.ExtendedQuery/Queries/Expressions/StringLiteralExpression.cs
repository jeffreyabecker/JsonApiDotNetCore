using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class StringLiteralExpression : LiteralConstantExpression
{
    public StringLiteralExpression(string stringValue) : base(stringValue, stringValue)
    {
    }
    public override TResult Accept<TArgument, TResult>(QueryExpressionVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.DefaultVisit(this, argument);
}
