using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal abstract class JoinNode : TableSourceNode
{
    public TableColumnNode JoinColumn { get; }
    public TableColumnNode ParentJoinColumn { get; }

    protected JoinNode(TableColumnNode joinColumn, TableColumnNode parentJoinColumn)
        : base(joinColumn.Table)
    {
        ArgumentGuard.NotNull(joinColumn);
        ArgumentGuard.NotNull(parentJoinColumn);

        JoinColumn = joinColumn;
        ParentJoinColumn = parentJoinColumn;
    }
}
