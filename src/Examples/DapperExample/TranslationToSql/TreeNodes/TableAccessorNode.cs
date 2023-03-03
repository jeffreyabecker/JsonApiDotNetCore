using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal abstract class TableAccessorNode : SqlTreeNode
{
    public TableNode Table { get; }

    protected TableAccessorNode(TableNode table)
    {
        ArgumentGuard.NotNull(table);

        Table = table;
    }
}
