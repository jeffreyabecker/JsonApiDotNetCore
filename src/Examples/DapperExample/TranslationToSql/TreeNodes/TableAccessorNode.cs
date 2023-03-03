using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal abstract class TableAccessorNode : SqlTreeNode
{
    public TableSourceNode TableSource { get; }

    protected TableAccessorNode(TableSourceNode tableSource)
    {
        ArgumentGuard.NotNull(tableSource);

        TableSource = tableSource;
    }
}
