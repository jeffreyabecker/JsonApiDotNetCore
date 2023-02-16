using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class OrderByCountNode : OrderByTermNode
{
    public CountNode Count { get; }

    public OrderByCountNode(CountNode count, bool isAscending)
        : base(isAscending)
    {
        ArgumentGuard.NotNull(count);

        Count = count;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitOrderByCount(this, argument);
    }
}
