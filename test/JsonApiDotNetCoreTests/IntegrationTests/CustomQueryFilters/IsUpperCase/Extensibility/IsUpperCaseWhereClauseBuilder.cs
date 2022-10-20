using System.Linq.Expressions;
using System.Reflection;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Queries.Internal.QueryableBuilding;

namespace JsonApiDotNetCoreTests.IntegrationTests.CustomQueryFilters.IsUpperCase.Extensibility;

internal sealed class IsUpperCaseWhereClauseBuilder : WhereClauseBuilder
{
    private static readonly MethodInfo ToUpperMethod = typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!;

    public IsUpperCaseWhereClauseBuilder(Expression source, LambdaScope lambdaScope, Type extensionType, LambdaParameterNameFactory nameFactory,
        IQueryableFactory queryableFactory)
        : base(source, lambdaScope, extensionType, nameFactory, queryableFactory)
    {
    }

    public override Expression DefaultVisit(QueryExpression expression, Type? argument)
    {
        if (expression is IsUpperCaseExpression isUpperCaseExpression)
        {
            return VisitIsUpperCase(isUpperCaseExpression, argument);
        }

        throw new NotSupportedException($"Unknown expression of type '{expression.GetType()}'.");
    }

    private Expression VisitIsUpperCase(IsUpperCaseExpression expression, Type? argument)
    {
        Expression attrPropertyAccess = Visit(expression.TargetAttribute, argument);
        MethodCallExpression attrToUpperMethodCall = Expression.Call(attrPropertyAccess, ToUpperMethod);

        return Expression.Equal(attrPropertyAccess, attrToUpperMethodCall);
    }
}
