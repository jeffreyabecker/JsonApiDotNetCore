using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class JoinNode : TableAccessorNode
{
    public JoinType JoinType { get; }
    public TableSourceNode JoinTableSource { get; }
    public ColumnNode JoinColumn { get; }
    public ColumnNode ParentJoinColumn { get; }

    public JoinNode(JoinType joinType, TableSourceNode joinTableSource, ColumnNode joinColumn, ColumnNode parentJoinColumn)
        : base(joinTableSource)
    {
        ArgumentGuard.NotNull(joinTableSource);
        ArgumentGuard.NotNull(joinColumn);
        ArgumentGuard.NotNull(parentJoinColumn);

        JoinType = joinType;
        JoinTableSource = joinTableSource;
        JoinColumn = joinColumn;
        ParentJoinColumn = parentJoinColumn;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitJoin(this, argument);
    }
}
