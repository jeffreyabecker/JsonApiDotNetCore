using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonApiDotNetCore.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
public class WrapperExpression : FilterExpression
{
    public WrapperExpression(ExtendedQueryExpression innerExpression)
    {
        InnerExpression = innerExpression;
    }

    public ExtendedQueryExpression InnerExpression { get; }

    public override TResult Accept<TArgument, TResult>(QueryExpressionVisitor<TArgument, TResult> visitor, TArgument argument) => visitor.DefaultVisit(this.InnerExpression, argument);

    public override string ToFullString() => InnerExpression.ToFullString();
}
