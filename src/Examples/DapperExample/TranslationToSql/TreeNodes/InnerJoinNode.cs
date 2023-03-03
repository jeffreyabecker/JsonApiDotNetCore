namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class InnerJoinNode : JoinNode
{
    public InnerJoinNode(TableSourceNode joinTableSource, ColumnNode joinColumn, ColumnNode parentJoinColumn)
        : base(joinTableSource, joinColumn, parentJoinColumn)
    {
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitInnerJoin(this, argument);
    }
}
