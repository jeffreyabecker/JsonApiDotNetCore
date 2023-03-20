using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal abstract class TableAccessorNode : SqlTreeNode
{
    public TableSourceNode Source { get; }

    protected TableAccessorNode(TableSourceNode source)
    {
        ArgumentGuard.NotNull(source);

        Source = source;
    }
}
