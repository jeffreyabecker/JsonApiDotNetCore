namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class InnerJoinNode : JoinNode
{
    public InnerJoinNode(TableNode joinTable, ColumnNode joinColumn, ColumnNode parentJoinColumn)
        : base(joinTable, joinColumn, parentJoinColumn)
    {
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitInnerJoin(this, argument);
    }
}
