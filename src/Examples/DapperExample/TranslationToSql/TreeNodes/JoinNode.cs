using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal abstract class JoinNode : TableAccessorNode
{
    public TableSourceNode JoinTableSource { get; }
    public ColumnNode JoinColumn { get; }
    public ColumnNode ParentJoinColumn { get; }

    protected JoinNode(TableSourceNode joinTableSource, ColumnNode joinColumn, ColumnNode parentJoinColumn)
        : base(joinTableSource)
    {
        ArgumentGuard.NotNull(joinTableSource);
        ArgumentGuard.NotNull(joinColumn);
        ArgumentGuard.NotNull(parentJoinColumn);

        JoinTableSource = joinTableSource;
        JoinColumn = joinColumn;
        ParentJoinColumn = parentJoinColumn;
    }
}
