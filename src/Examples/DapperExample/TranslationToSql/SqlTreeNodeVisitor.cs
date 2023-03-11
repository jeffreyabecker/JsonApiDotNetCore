using DapperExample.TranslationToSql.TreeNodes;

namespace DapperExample.TranslationToSql;

internal abstract class SqlTreeNodeVisitor<TArgument, TResult>
{
    public virtual TResult Visit(SqlTreeNode node, TArgument argument)
    {
        return node.Accept(this, argument);
    }

    public virtual TResult DefaultVisit(SqlTreeNode node, TArgument argument)
    {
        return default!;
    }

    public virtual TResult VisitSelect(SelectNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitInsert(InsertNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitUpdate(UpdateNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitDelete(DeleteNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitTable(TableNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitFrom(FromNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitJoin(JoinNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitColumn(ColumnNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitColumnSelector(ColumnSelectorNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitOneSelector(OneSelectorNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitCountSelector(CountSelectorNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitRowNumber(RowNumberNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitNot(NotNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitLogical(LogicalNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitComparison(ComparisonNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitLike(LikeNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitIn(InNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitExists(ExistsNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitCount(CountNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitOrderBy(OrderByNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitOrderByColumn(OrderByColumnNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitOrderByCount(OrderByCountNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitLimitOffset(LimitOffsetNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitColumnAssignment(ColumnAssignmentNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitParameter(ParameterNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }

    public virtual TResult VisitNullConstant(NullConstantNode node, TArgument argument)
    {
        return DefaultVisit(node, argument);
    }
}