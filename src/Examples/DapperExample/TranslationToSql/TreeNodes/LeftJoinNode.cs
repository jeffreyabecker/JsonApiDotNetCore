namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class LeftJoinNode : JoinNode
{
    public LeftJoinNode(TableSourceNode joinTableSource, ColumnNode joinColumn, ColumnNode parentJoinColumn)
        : base(joinTableSource, joinColumn, parentJoinColumn)
    {
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitLeftJoin(this, argument);
    }
}
