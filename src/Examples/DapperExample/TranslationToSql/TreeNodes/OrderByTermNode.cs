namespace DapperExample.TranslationToSql.TreeNodes;

internal abstract class OrderByTermNode : SqlTreeNode
{
    public bool IsAscending { get; }

    protected OrderByTermNode(bool isAscending)
    {
        IsAscending = isAscending;
    }
}
