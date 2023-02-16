using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal abstract class TableSourceNode : SqlTreeNode
{
    public TableNode Table { get; }

    protected TableSourceNode(TableNode table)
    {
        ArgumentGuard.NotNull(table);

        Table = table;
    }
}
