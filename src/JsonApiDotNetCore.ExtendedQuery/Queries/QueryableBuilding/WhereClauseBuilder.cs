using System.Linq.Expressions;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Queries.QueryableBuilding;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.QueryableBuilding;
public partial class WhereClauseBuilder : QueryExpressionVisitor<QueryClauseBuilderContext, Expression>, IWhereClauseBuilder
{
    public Expression ApplyWhere(FilterExpression filter, QueryClauseBuilderContext context)
    {
        throw new NotImplementedException();
    }

    public override Expression DefaultVisit(QueryExpression expression, QueryClauseBuilderContext argument)
    {
        return default!;
    }
}
