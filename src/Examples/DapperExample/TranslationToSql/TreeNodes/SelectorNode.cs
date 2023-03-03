namespace DapperExample.TranslationToSql.TreeNodes;

internal abstract class SelectorNode : SqlTreeNode
{
    public string? Alias { get; }

    protected SelectorNode(string? alias)
    {
        Alias = alias;
    }
}
