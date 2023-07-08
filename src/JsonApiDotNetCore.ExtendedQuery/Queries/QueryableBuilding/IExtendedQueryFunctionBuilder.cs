using System.Collections.Immutable;
using System.Linq.Expressions;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.Queries.QueryableBuilding;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.QueryableBuilding;
public interface IExtendedQueryFunctionBuilder
{
    public string ForFunctionName { get; set; }
    public IEnumerable<ExtendedQueryExpression> RewriteParams(IImmutableList<ExtendedQueryExpression> functionParams);
    public Expression? VisitExtendedQueryExpression(IVisitExtendedQueryExpressions visitor, ExtendedQueryExpression expression, QueryClauseBuilderContext context);
    public Expression BuildExpression(IImmutableList<Expression> functionParams);

}
