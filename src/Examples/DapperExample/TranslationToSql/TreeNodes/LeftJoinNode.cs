namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class LeftJoinNode : JoinNode
{
    public LeftJoinNode(TableColumnNode joinColumn, TableColumnNode parentJoinColumn)
        : base(joinColumn, parentJoinColumn)
    {
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitLeftJoin(this, argument);
    }
}
