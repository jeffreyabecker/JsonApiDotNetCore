namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class CountSelectorNode : SelectorNode
{
    public static readonly CountSelectorNode Instance = new();

    private CountSelectorNode()
    {
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitCountSelector(this, argument);
    }
}
