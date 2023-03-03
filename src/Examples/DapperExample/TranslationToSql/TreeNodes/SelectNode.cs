namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class SelectNode : SqlTreeNode
{
    public SelectShape SelectShape { get; }
    public IReadOnlyDictionary<TableSourceNode, IReadOnlyList<ColumnNode>> SelectedColumns { get; }
    public FilterNode? Where { get; }
    public OrderByNode? OrderBy { get; }
    public LimitOffsetNode? LimitOffset { get; }

    public SelectNode(SelectShape selectShape, IReadOnlyDictionary<TableSourceNode, IReadOnlyList<ColumnNode>> selectedColumns, FilterNode? where,
        OrderByNode? orderBy, LimitOffsetNode? limitOffset)
    {
        SelectShape = selectShape;
        SelectedColumns = selectedColumns;
        Where = where;
        OrderBy = orderBy;
        LimitOffset = limitOffset;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitSelect(this, argument);
    }
}
