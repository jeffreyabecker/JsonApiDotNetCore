namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class CountSelectorNode : SelectorNode
{
    public CountSelectorNode(string? alias)
        : base(alias)
    {
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitCountSelector(this, argument);
    }
}
