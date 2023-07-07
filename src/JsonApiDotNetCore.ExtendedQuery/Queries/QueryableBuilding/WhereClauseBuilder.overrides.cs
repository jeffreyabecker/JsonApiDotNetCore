using System.Linq.Expressions;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Queries.QueryableBuilding;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.QueryableBuilding;
public partial class WhereClauseBuilder
{
    public class ShouldntEverCallThisException : Exception
    {
        public ShouldntEverCallThisException(Type type): base($"Something went deeply wrong because the visitor for {type.FullName} got explicitly called") { }
    }
    public override Expression VisitComparison(ComparisonExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitResourceFieldChain(ResourceFieldChainExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitLiteralConstant(LiteralConstantExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitNullConstant(NullConstantExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitLogical(LogicalExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitNot(NotExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitHas(HasExpr expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitIsType(IsTypeExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitSortElement(SortElementExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitSort(SortExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitPagination(PaginationExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitCount(CountExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitMatchText(MatchTextExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitAny(AnyExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitSparseFieldTable(SparseFieldTableExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitSparseFieldSet(SparseFieldSetExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitQueryStringParameterScope(QueryStringParameterScopeExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression PaginationQueryStringValue(PaginationQueryStringValueExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression PaginationElementQueryStringValue(PaginationElementQueryStringValueExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitInclude(IncludeExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitIncludeElement(IncludeElementExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }

    public override Expression VisitQueryableHandler(QueryableHandlerExpression expression, QueryClauseBuilderContext argument)
    {
        throw new ShouldntEverCallThisException(expression.GetType());
    }
}
