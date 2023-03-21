using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class JoinNode : TableAccessorNode
{
    public JoinType JoinType { get; }
    public ColumnNode JoinColumn { get; }
    public ColumnNode ParentJoinColumn { get; }

    public JoinNode(JoinType joinType, TableSourceNode source, ColumnNode joinColumn, ColumnNode parentJoinColumn)
        : base(source)
    {
        ArgumentGuard.NotNull(joinColumn);
        ArgumentGuard.NotNull(parentJoinColumn);

        JoinType = joinType;
        JoinColumn = joinColumn;
        ParentJoinColumn = parentJoinColumn;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitJoin(this, argument);
    }
}
