using System.Collections;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Errors;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;

namespace JsonApiDotNetCore.Queries.QueryableBuilding;

/// <inheritdoc cref="IWhereClauseBuilder" />
[PublicAPI]
public class WhereClauseBuilder : QueryClauseBuilder, IWhereClauseBuilder<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection>
{
    private static readonly CollectionConverter CollectionConverter = new();
    private static readonly ConstantExpression NullConstant = Expression.Constant(null);

    public virtual Expression ApplyWhere(FilterExpression filter, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        ArgumentGuard.NotNull(filter);

        LambdaExpression lambda = GetPredicateLambda(filter, context);

        return WhereExtensionMethodCall(lambda, context);
    }

    private LambdaExpression GetPredicateLambda(FilterExpression filter, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        Expression body = Visit(filter, context);
        return Expression.Lambda(body, context.LambdaScope.Parameter);
    }

    private static Expression WhereExtensionMethodCall(LambdaExpression predicate, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        return Expression.Call(context.ExtensionType, "Where", context.LambdaScope.Parameter.Type.AsArray(), context.Source, predicate);
    }

    public override Expression VisitHas(HasExpression expression, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        Expression property = Visit(expression.TargetCollection, context);

        Type? elementType = CollectionConverter.FindCollectionElementType(property.Type);

        if (elementType == null)
        {
            throw new InvalidOperationException("Expression must be a collection.");
        }

        Expression? predicate = null;

        if (expression.Filter != null)
        {
            ResourceType resourceType = ((HasManyAttribute)expression.TargetCollection.Fields[^1]).RightType;

            using LambdaScope lambdaScope = context.LambdaScopeFactory.CreateScope(elementType);

            var nestedContext = new QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection>(property, resourceType, typeof(Enumerable), context.EntityModel, context.LambdaScopeFactory,
                lambdaScope, context.QueryableBuilder, context.State);

            predicate = GetPredicateLambda(expression.Filter, nestedContext);
        }

        return AnyExtensionMethodCall(elementType, property, predicate);
    }

    private static MethodCallExpression AnyExtensionMethodCall(Type elementType, Expression source, Expression? predicate)
    {
        return predicate != null
            ? Expression.Call(typeof(Enumerable), "Any", elementType.AsArray(), source, predicate)
            : Expression.Call(typeof(Enumerable), "Any", elementType.AsArray(), source);
    }

    public override Expression VisitIsType(IsTypeExpression expression, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        Expression property = expression.TargetToOneRelationship != null ? Visit(expression.TargetToOneRelationship, context) : context.LambdaScope.Accessor;
        TypeBinaryExpression typeCheck = Expression.TypeIs(property, expression.DerivedType.ClrType);

        if (expression.Child == null)
        {
            return typeCheck;
        }

        UnaryExpression derivedAccessor = Expression.Convert(property, expression.DerivedType.ClrType);

        QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> derivedContext = context.WithLambdaScope(context.LambdaScope.WithAccessor(derivedAccessor));
        Expression filter = Visit(expression.Child, derivedContext);

        return Expression.AndAlso(typeCheck, filter);
    }

    public override Expression VisitMatchText(MatchTextExpression expression, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        Expression property = Visit(expression.TargetAttribute, context);

        if (property.Type != typeof(string))
        {
            throw new InvalidOperationException("Expression must be a string.");
        }

        Expression text = Visit(expression.TextValue, context);

        if (expression.MatchKind == TextMatchKind.StartsWith)
        {
            return Expression.Call(property, "StartsWith", null, text);
        }

        if (expression.MatchKind == TextMatchKind.EndsWith)
        {
            return Expression.Call(property, "EndsWith", null, text);
        }

        return Expression.Call(property, "Contains", null, text);
    }

    public override Expression VisitAny(AnyExpression expression, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        Expression property = Visit(expression.TargetAttribute, context);

        var valueList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(property.Type))!;

        foreach (LiteralConstantExpression constant in expression.Constants)
        {
            valueList.Add(constant.TypedValue);
        }

        ConstantExpression collection = Expression.Constant(valueList);
        return ContainsExtensionMethodCall(collection, property);
    }

    private static Expression ContainsExtensionMethodCall(Expression collection, Expression value)
    {
        return Expression.Call(typeof(Enumerable), "Contains", value.Type.AsArray(), collection, value);
    }

    public override Expression VisitLogical(LogicalExpression expression, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        var termQueue = new Queue<Expression>(expression.Terms.Select(filter => Visit(filter, context)));

        if (expression.Operator == LogicalOperator.And)
        {
            return Compose(termQueue, Expression.AndAlso);
        }

        if (expression.Operator == LogicalOperator.Or)
        {
            return Compose(termQueue, Expression.OrElse);
        }

        throw new InvalidOperationException($"Unknown logical operator '{expression.Operator}'.");
    }

    private static BinaryExpression Compose(Queue<Expression> argumentQueue, Func<Expression, Expression, BinaryExpression> applyOperator)
    {
        Expression left = argumentQueue.Dequeue();
        Expression right = argumentQueue.Dequeue();

        BinaryExpression tempExpression = applyOperator(left, right);

        while (argumentQueue.Any())
        {
            Expression nextArgument = argumentQueue.Dequeue();
            tempExpression = applyOperator(tempExpression, nextArgument);
        }

        return tempExpression;
    }

    public override Expression VisitNot(NotExpression expression, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        Expression child = Visit(expression.Child, context);
        return Expression.Not(child);
    }

    public override Expression VisitComparison(ComparisonExpression expression, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        Type commonType = ResolveCommonType(expression.Left, expression.Right, context);

        Expression left = WrapInConvert(Visit(expression.Left, context), commonType);
        Expression right = WrapInConvert(Visit(expression.Right, context), commonType);

        return expression.Operator switch
        {
            ComparisonOperator.Equals => Expression.Equal(left, right),
            ComparisonOperator.LessThan => Expression.LessThan(left, right),
            ComparisonOperator.LessOrEqual => Expression.LessThanOrEqual(left, right),
            ComparisonOperator.GreaterThan => Expression.GreaterThan(left, right),
            ComparisonOperator.GreaterOrEqual => Expression.GreaterThanOrEqual(left, right),
            _ => throw new InvalidOperationException($"Unknown comparison operator '{expression.Operator}'.")
        };
    }

    private Type ResolveCommonType(QueryExpression left, QueryExpression right, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        Type leftType = ResolveFixedType(left, context);

        if (RuntimeTypeConverter.CanContainNull(leftType))
        {
            return leftType;
        }

        if (right is NullConstantExpression)
        {
            return typeof(Nullable<>).MakeGenericType(leftType);
        }

        Type? rightType = TryResolveFixedType(right, context);

        if (rightType != null && RuntimeTypeConverter.CanContainNull(rightType))
        {
            return rightType;
        }

        return leftType;
    }

    private Type ResolveFixedType(QueryExpression expression, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        Expression result = Visit(expression, context);
        return result.Type;
    }

    private Type? TryResolveFixedType(QueryExpression expression, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        if (expression is CountExpression)
        {
            return typeof(int);
        }

        if (expression is ResourceFieldChainExpression chain)
        {
            Expression child = Visit(chain, context);
            return child.Type;
        }

        return null;
    }

    private static Expression WrapInConvert(Expression expression, Type targetType)
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

    public override Expression VisitNullConstant(NullConstantExpression expression, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        return NullConstant;
    }

    public override Expression VisitLiteralConstant(LiteralConstantExpression expression, QueryClauseBuilderContext<QueryLayer, IncludeExpression, FilterExpression, SortExpression, PaginationExpression, FieldSelection> context)
    {
        Type type = expression.TypedValue.GetType();
        return expression.TypedValue.CreateTupleAccessExpressionForConstant(type);
    }
}
