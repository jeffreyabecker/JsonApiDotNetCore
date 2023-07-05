using System.Linq.Expressions;
using JsonApiDotNetCore;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Queries;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Queries.QueryableBuilding;
using JsonApiDotNetCore.Resources.Annotations;

namespace JsonApiDotNetCoreTests.IntegrationTests.QueryStrings.CustomFunctions.Sum;

internal sealed class SumWhereClauseBuilder : WhereClauseBuilder
{
    public override Expression DefaultVisit(QueryExpression expression, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        if (expression is SumExpression sumExpression)
        {
            return VisitSum(sumExpression, context);
        }

        return base.DefaultVisit(expression, context);
    }

    private Expression VisitSum(SumExpression expression, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        Expression collectionPropertyAccess = Visit(expression.TargetToManyRelationship, context);

        ResourceType selectorResourceType = ((HasManyAttribute)expression.TargetToManyRelationship.Fields.Single()).RightType;
        using LambdaScope lambdaScope = context.LambdaScopeFactory.CreateScope(selectorResourceType.ClrType);

        var nestedContext = new QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection>(collectionPropertyAccess, selectorResourceType, typeof(Enumerable), context.EntityModel,
            context.LambdaScopeFactory, lambdaScope, context.QueryableBuilder, context.State);

        LambdaExpression lambda = GetSelectorLambda(expression.Selector, nestedContext);

        return SumExtensionMethodCall(lambda, nestedContext);
    }

    private LambdaExpression GetSelectorLambda(QueryExpression expression, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        Expression body = Visit(expression, context);
        return Expression.Lambda(body, context.LambdaScope.Parameter);
    }

    private static Expression SumExtensionMethodCall(LambdaExpression selector, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        return Expression.Call(context.ExtensionType, "Sum", context.LambdaScope.Parameter.Type.AsArray(), context.Source, selector);
    }
}
