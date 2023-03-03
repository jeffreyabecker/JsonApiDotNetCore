namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class OneSelectorNode : SelectorNode
{
    public OneSelectorNode(string? alias)
        : base(alias)
    {
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitOneSelector(this, argument);
    }
}
