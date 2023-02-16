using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class CountNode : FilterNode
{
    public SelectNode SubSelect { get; }

    public CountNode(SelectNode subSelect)
    {
        ArgumentGuard.NotNull(subSelect);

        SubSelect = subSelect;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitCount(this, argument);
    }
}
