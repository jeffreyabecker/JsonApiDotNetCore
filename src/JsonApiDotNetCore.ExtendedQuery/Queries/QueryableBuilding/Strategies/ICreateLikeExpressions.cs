using System.Linq.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.QueryableBuilding.Strategies;

public interface ICreateLikeExpressions
{
    Expression CreateLike(Expression left, Expression right);
}
