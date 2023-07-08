using System.Linq.Expressions;
using JsonApiDotNetCore.Errors;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.QueryableBuilding.Strategies;
public static class LinqExpressionExtensions
{
    public static Expression WrapInConvert(this Expression expression, Type targetType)
    {
        try
        {
            return expression.Type != targetType ? Expression.Convert(expression, targetType) : expression;
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidQueryException("Query creation failed due to incompatible types.", exception);
        }
    }
}
