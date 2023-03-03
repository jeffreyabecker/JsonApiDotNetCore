using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal abstract class JoinNode : TableSourceNode
{
    public TableNode JoinTable { get; }
    public ColumnNode JoinColumn { get; }
    public ColumnNode ParentJoinColumn { get; }

    protected JoinNode(TableNode joinTable, ColumnNode joinColumn, ColumnNode parentJoinColumn)
        : base(joinTable)
    {
        ArgumentGuard.NotNull(joinTable);
        ArgumentGuard.NotNull(joinColumn);
        ArgumentGuard.NotNull(parentJoinColumn);

        JoinTable = joinTable;
        JoinColumn = joinColumn;
        ParentJoinColumn = parentJoinColumn;
    }
}
