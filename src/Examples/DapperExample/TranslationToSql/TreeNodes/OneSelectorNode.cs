namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class OneSelectorNode : SelectorNode
{
    public static readonly OneSelectorNode Instance = new();

    private OneSelectorNode()
    {
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitOneSelector(this, argument);
    }
}
