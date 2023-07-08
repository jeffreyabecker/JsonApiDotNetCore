using System.Collections;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.Queries.QueryableBuilding;
using Microsoft.EntityFrameworkCore;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.QueryableBuilding.Strategies;
public class BinaryStrategy : IWhereClauseBuilderStrategy<BinaryFilterExpression>
{
    private readonly ICreateLikeExpressions _likeFactory;

    public BinaryStrategy(ICreateLikeExpressions likeFactory)
    {
        _likeFactory = likeFactory;
    }
    public class RelationalLikeFactory : ICreateLikeExpressions
    {
        public Expression CreateLike(Expression left, Expression right) => Expression.Call(typeof(DbFunctionsExtensions), "Like", new Type[] { typeof(string), typeof(string) }, left, right);
    }
    public class MemoryLikeFactory : ICreateLikeExpressions
    {
        private static readonly Type[] NoTypes = new Type[0];
        public Expression CreateLike(Expression leftExpr, Expression rightExpr)
        {            
            var left = leftExpr.WrapInConvert(typeof(string));
            var right = rightExpr.WrapInConvert(typeof(string));
            var escaped = Expression.Call(typeof(Regex), "Escape", NoTypes, right);
            var percentToGlob = Expression.Call(escaped, "Replace", NoTypes, Expression.Constant("%"), Expression.Constant(".*?") );
            var regex = Expression.Call(percentToGlob, "Replace", NoTypes,  Expression.Constant("_"), Expression.Constant(".") );
            return Expression.Call(typeof(Regex), "IsMatch", NoTypes, left, regex);
        }
    }
    public Expression Visit(IVisitExtendedQueryExpressions visitor, BinaryFilterExpression binaryExpr, QueryClauseBuilderContext context)
    {
        if (binaryExpr.Operator == BinaryFilterOperator.In || binaryExpr.Operator == BinaryFilterOperator.NotIn)
        {
            return VisitInExpression(visitor, binaryExpr.Operator == BinaryFilterOperator.NotIn, binaryExpr.Left, (ExpressionListExpression)binaryExpr.Right, context);
        }
        var left = visitor.VisitExtendedQueryExpression(binaryExpr.Left, context);
        var right = visitor.VisitExtendedQueryExpression(binaryExpr.Right, context);
        Type commonType = visitor.ResolveCommonType(binaryExpr.Left, binaryExpr.Right, context);
        left = left.WrapInConvert(commonType);
        right = right.WrapInConvert(commonType);


        if (binaryExpr.Operator == BinaryFilterOperator.Add)
        {
            return Expression.Add(left, right);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.Subtract)
        {
            return Expression.Subtract(left, right);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.Multiply)
        {
            return Expression.Multiply(left, right);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.Divide)
        {
            return Expression.Divide(left, right);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.Modulo)
        {
            return Expression.Modulo(left, right);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.GreaterThan)
        {
            return Expression.GreaterThan(left, right);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.LessThan)
        {
            return Expression.LessThan(left, right);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.GreaterThanOrEqual)
        {
            return Expression.GreaterThanOrEqual(left, right);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.LessThanOrEqual)
        {
            return Expression.LessThanOrEqual(left, right);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.Equal)
        {
            return Expression.Equal(left, right);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.NotEqual)
        {
            return Expression.NotEqual(left, right);
        }

        if (binaryExpr.Operator == BinaryFilterOperator.Like)
        {
            return _likeFactory.CreateLike(left,right);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.NotLike)
        {
            return Expression.Not(_likeFactory.CreateLike(left, right));
        }

        if (binaryExpr.Operator == BinaryFilterOperator.And)
        {
            return Expression.AndAlso(left, right);
        }
        if (binaryExpr.Operator == BinaryFilterOperator.Or)
        {
            return Expression.OrElse(left, right);
        }
        throw new InvalidOperationException($"Unknown comparison operator '{binaryExpr.Operator}'.");
    }
    public Expression VisitInExpression(IVisitExtendedQueryExpressions visitor, bool notIn, ExtendedQueryExpression leftExpr, ExpressionListExpression rightExpr, QueryClauseBuilderContext context)
    {
        var leftType = visitor.ResolveFixedType(leftExpr, context);
        var needle = visitor.VisitExtendedQueryExpression(leftExpr, context);

        var valueList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(needle.Type))!;
        foreach (var listElement in rightExpr.Expressions)
        {
            if (listElement is LiteralQueryExpression literal)
            {
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
}
