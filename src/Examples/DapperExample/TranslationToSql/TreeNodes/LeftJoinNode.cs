namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class LeftJoinNode : JoinNode
{
    public LeftJoinNode(TableNode joinTable, ColumnNode joinColumn, ColumnNode parentJoinColumn)
        : base(joinTable, joinColumn, parentJoinColumn)
    {
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitLeftJoin(this, argument);
    }
}
