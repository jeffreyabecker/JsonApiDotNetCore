using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class NotNode : FilterNode
{
    public FilterNode Child { get; }

    public NotNode(FilterNode child)
    {
        ArgumentGuard.NotNull(child);

        Child = child;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitNot(this, argument);
    }
}
