using System.Collections;
using System.Linq.Expressions;
using JsonApiDotNetCore.Errors;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;

using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;
using Microsoft.EntityFrameworkCore;

using QueryClauseBuilderContext = JsonApiDotNetCore.Queries.QueryableBuilding.QueryClauseBuilderContext;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.QueryableBuilding;
public partial class WhereClauseBuilder : JsonApiDotNetCore.Queries.Expressions.QueryExpressionVisitor<JsonApiDotNetCore.Queries.QueryableBuilding.QueryClauseBuilderContext, Expression>, JsonApiDotNetCore.Queries.QueryableBuilding.IWhereClauseBuilder
{
    public Expression ApplyWhere(JsonApiDotNetCore.Queries.Expressions.FilterExpression filter, QueryClauseBuilderContext context)
    {
        if(filter is WrapperExpression wrapper)
        {
            return VisitExtendedQueryExpression(wrapper.InnerExpression, context);
        }
        throw new JsonApiDotNetCore.Queries.Parsing.QueryParseException("Invalid fitler expression type -- is your DI container configured correctly", 0);
    }
    public override Expression DefaultVisit(JsonApiDotNetCore.Queries.Expressions.QueryExpression expression, QueryClauseBuilderContext context)
    {
        if(expression is EmptyExpression)
        {
            return context.Source;
        }
        if(expression is ExtendedQueryExpression ext) {
            return VisitExtendedQueryExpression(ext, context);
        }
        if (expression is WrapperExpression wrapper)
        {
            return VisitExtendedQueryExpression(wrapper.InnerExpression, context);
        }
        throw new NotImplementedException($"I dont know how to interpret a {expression.GetType().Name}");
    }

    public Expression VisitExtendedQueryExpression(ExtendedQueryExpression expression, QueryClauseBuilderContext context)
    {
        switch (expression)
        {
            case BinaryFilterExpression binaryExpr: return VisitBinaryFilterExpression(binaryExpr, context);
            case ConditionalFilterExpression conditional: return VisitConditionalFilterExpression(conditional, context);
            case EmptyExpression _: throw new NotImplementedException("not sure what to do here");          
            case FunctionCallExpression callExpr: return VisitFunctionCallExpression(callExpr, context);
            case HasRelatedExpression hasExpr: return VisitHasRelatedExpression(hasExpr, context);
            case IsOfTypeExpression isaExpr: return VisitIsOfTypeExpression(isaExpr, context);
            case LiteralQueryExpression literalExpr: return VisitLiteralQueryExpression(literalExpr, context);
            case ParentheticalExpression parenExpr: return VisitParentheticalExpression(parenExpr, context);
            case UnaryFilterExpression unaryExpr: return VisitUnaryFilterExpression(unaryExpr, context);
            case IdentifierExpression identExpr: return VisitIdentifierExpression(identExpr, context);

        }
        return default!;
    }
    public Expression VisitIdentifierExpression(IdentifierExpression identifierExpression, QueryClauseBuilderContext context)
    {
        MemberExpression? property = null;

        foreach (ResourceFieldAttribute field in identifierExpression.Fields)
        {
            Expression parentAccessor = property ?? context.LambdaScope.Accessor;
            Type propertyType = field.Property.DeclaringType!;
            string propertyName = field.Property.Name;

            bool requiresUpCast = parentAccessor.Type != propertyType && parentAccessor.Type.IsAssignableFrom(propertyType);
            Type parentType = requiresUpCast ? propertyType : parentAccessor.Type;

            if (parentType.GetProperty(propertyName) == null)
            {
                throw new InvalidOperationException($"Type '{parentType.Name}' does not contain a property named '{propertyName}'.");
            }

            property = requiresUpCast
                ? Expression.MakeMemberAccess(Expression.Convert(parentAccessor, propertyType), field.Property)
                : Expression.Property(parentAccessor, propertyName);
        }

        return property!;
    }


    public Expression VisitUnaryFilterExpression(UnaryFilterExpression unaryExpr, QueryClauseBuilderContext context)
    {
        var operand = VisitExtendedQueryExpression(unaryExpr.Operand, context);

        if(unaryExpr.Operator == UnaryFilterOperator.Not)
        {
            return Expression.Not(operand);
        }
        if(unaryExpr.Operator == UnaryFilterOperator.IsNotNull)
        {
            return Expression.NotEqual(operand, Expression.Constant(null));
        }
        if (unaryExpr.Operator == UnaryFilterOperator.IsNull)
        {
            return Expression.Equal(operand, Expression.Constant(null));
        }
        throw new NotImplementedException($"I dont know how to convert a {unaryExpr.Operator} expression");
    }

    public Expression VisitParentheticalExpression(ParentheticalExpression parenExpr, QueryClauseBuilderContext context)
    {        
        return VisitExtendedQueryExpression(parenExpr.Inner, context);
    }

    public Expression VisitLiteralQueryExpression(LiteralQueryExpression literalExpr, QueryClauseBuilderContext context)
    {
        return Expression.Constant(literalExpr.GetRawValue());
    }

    public Expression VisitIsOfTypeExpression(IsOfTypeExpression isaExpr, QueryClauseBuilderContext context)
    {
        throw new NotImplementedException();
    }

    public Expression VisitHasRelatedExpression(HasRelatedExpression hasExpr, QueryClauseBuilderContext context)
    {
        throw new NotImplementedException();
    }

    public Expression VisitFunctionCallExpression(FunctionCallExpression callExpr, QueryClauseBuilderContext context)
    {
        throw new NotImplementedException();
    }

    public Expression VisitConditionalFilterExpression(ConditionalFilterExpression conditional, QueryClauseBuilderContext context)
    {
        var condition = VisitExtendedQueryExpression(conditional.Condition, context);
        var whenTrue = VisitExtendedQueryExpression(conditional.WhenTrue, context);
        var whenFalse = VisitExtendedQueryExpression(conditional.WhenFalse, context);
        return Expression.Condition(condition, whenTrue, whenFalse);
    }

    public Expression VisitInExpression(bool notIn, ExtendedQueryExpression leftExpr, ExpressionListExpression rightExpr, QueryClauseBuilderContext context)
    {
        var leftType = ResolveFixedType(leftExpr, context);
        var needle = VisitExtendedQueryExpression(leftExpr, context);

        var valueList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(needle.Type))!;
        foreach (var listElement in rightExpr.Expressions)
        {
            if(listElement is LiteralQueryExpression literal){
                valueList.Add(Convert.ChangeType(literal.GetRawValue(), leftType));
            }
            else
            {
                throw new InvalidOperationException("The right hand side of the in operator can only contain constants");
            }
        }        
        var collection = Expression.Constant(valueList);
        return Expression.Call(typeof(Enumerable), "Contains", needle.Type.AsArray(), collection, needle);

    }
    public Expression VisitBinaryFilterExpression(BinaryFilterExpression binaryExpr, QueryClauseBuilderContext context)
    {

        if (binaryExpr.Operator == BinaryFilterOperator.In || binaryExpr.Operator == BinaryFilterOperator.NotIn) {
            return VisitInExpression(binaryExpr.Operator == BinaryFilterOperator.NotIn, binaryExpr.Left, (ExpressionListExpression)binaryExpr.Right, context);    
        }
        var lhs = VisitExtendedQueryExpression(binaryExpr.Left, context);
        var rhs = VisitExtendedQueryExpression(binaryExpr.Right, context);
        Type commonType = ResolveCommonType(binaryExpr.Left, binaryExpr.Right, context);
        lhs = WrapInConvert(lhs, commonType);
        rhs = WrapInConvert(rhs, commonType);


        if (binaryExpr.Operator == BinaryFilterOperator.Add) {
            return Expression.Add(lhs, rhs);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.Subtract) {
            return Expression.Subtract(lhs, rhs);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.Multiply) {
            return Expression.Multiply(lhs, rhs);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.Divide) {
            return Expression.Divide(lhs, rhs);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.Modulo) {
            return Expression.Modulo(lhs, rhs);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.GreaterThan) {
            return Expression.GreaterThan(lhs, rhs);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.LessThan) {
            return Expression.LessThan(lhs, rhs);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.GreaterThanOrEqual)
        {
            return Expression.GreaterThanOrEqual(lhs, rhs);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.LessThanOrEqual)
        {
            return Expression.LessThanOrEqual(lhs, rhs);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.Equal) {
            return Expression.Equal(lhs, rhs);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.NotEqual) {
            return Expression.NotEqual(lhs, rhs);
        }

        if (binaryExpr.Operator == BinaryFilterOperator.Like) {
            return CallLike(lhs,rhs);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.NotLike ) {
            return Expression.Not(CallLike(lhs, rhs));
        }

        if (binaryExpr.Operator == BinaryFilterOperator.And) {
            return Expression.AndAlso(lhs, rhs);    
        }
        if (binaryExpr.Operator == BinaryFilterOperator.Or) {
            return Expression.OrElse(lhs, rhs);  
        }
        throw new InvalidOperationException($"Unknown comparison operator '{binaryExpr.Operator}'.");
    }
    private static Expression CallLike(Expression lhs, Expression rhs)
    {
        return Expression.Call(typeof(DbFunctionsExtensions), "Like", new Type[] { typeof(string), typeof(string) }, lhs, rhs);
    }

    protected Type ResolveCommonType(ExtendedQueryExpression left, ExtendedQueryExpression right, QueryClauseBuilderContext context)
    {
        Type leftType = ResolveFixedType(left, context);

        if (RuntimeTypeConverter.CanContainNull(leftType))
        {
            return leftType;
        }

        if (right is LiteralQueryExpression.NullLiteralExpression)
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
    protected Type ResolveFixedType(ExtendedQueryExpression expression, QueryClauseBuilderContext context)
    {
        Expression result = VisitExtendedQueryExpression(expression, context);
        return result.Type;
    }
    protected Type? TryResolveFixedType(ExtendedQueryExpression expression, QueryClauseBuilderContext context)
    {
        if (expression is IdentifierExpression chain)
        {
            Expression child = VisitIdentifierExpression(chain, context);
            return child.Type;
        }

        return null;
    }

    protected static Expression WrapInConvert(Expression expression, Type targetType)
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
