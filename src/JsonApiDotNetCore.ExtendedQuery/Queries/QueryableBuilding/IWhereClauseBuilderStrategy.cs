using System.Linq.Expressions;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.Queries.QueryableBuilding;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.QueryableBuilding;
public interface IWhereClauseBuilderStrategy
{
    Type ForType { get; }
    Expression Visit(IVisitExtendedQueryExpressions visitor, ExtendedQueryExpression expression, QueryClauseBuilderContext context);
}
public interface IWhereClauseBuilderStrategy<TQueryExpression> : IWhereClauseBuilderStrategy
    where TQueryExpression : ExtendedQueryExpression
{
    Type IWhereClauseBuilderStrategy.ForType=> typeof(TQueryExpression);
    Expression Visit(IVisitExtendedQueryExpressions visitor, TQueryExpression expression, QueryClauseBuilderContext context);
    Expression IWhereClauseBuilderStrategy.Visit(IVisitExtendedQueryExpressions visitor, ExtendedQueryExpression expression, QueryClauseBuilderContext context) => Visit(visitor, (TQueryExpression)expression, context);
}
