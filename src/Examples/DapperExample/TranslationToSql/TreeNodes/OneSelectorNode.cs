namespace DapperExample.TranslationToSql.TreeNodes;

/// <summary>
/// Represents the ordinal selector for the first, unnamed column in a <see cref="SelectNode"/>. For example, "1" in: SELECT 1 FROM Users
/// </summary>
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
