using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class ExistsNode : FilterNode
{
    public SelectNode SubSelect { get; }

    public ExistsNode(SelectNode subSelect)
    {
        ArgumentGuard.NotNull(subSelect);

        SubSelect = subSelect;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitExists(this, argument);
    }
}
