
using System.Collections.Immutable;
using System.Linq.Expressions;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.QueryableBuilding.Strategies;
public class FunctionCallStrategy : IWhereClauseBuilderStrategy<FunctionCallExpression>
{
    private readonly IReadOnlyDictionary<string, IExtendedQueryFunctionBuilder> _functionBuilders;

    public FunctionCallStrategy(ICollection<IExtendedQueryFunctionBuilder> functionBuilders)
    {
        _functionBuilders = functionBuilders.ToImmutableDictionary(x=>x.ForFunctionName, x=>x, StringComparer.OrdinalIgnoreCase);
    }
    public Expression Visit(IVisitExtendedQueryExpressions visitor, FunctionCallExpression callExpr, JsonApiDotNetCore.Queries.QueryableBuilding.QueryClauseBuilderContext context)
    {
        if (!_functionBuilders.ContainsKey(callExpr.Name))
        {
            throw new NotImplementedException($"The function {callExpr.Name} does not have a registered function builder. Maybe implement a IExtendedQueryFunctionBuilder for it?");
        }
        var builder = _functionBuilders[callExpr.Name];
        var queryParams = builder.RewriteParams(callExpr.Arguments.Expressions);
        var exprParams = queryParams.Select(qp => builder.VisitExtendedQueryExpression(visitor, qp, context) ?? visitor.VisitExtendedQueryExpression(qp, context)).ToImmutableList();
        return builder.BuildExpression(exprParams);
    }
}
