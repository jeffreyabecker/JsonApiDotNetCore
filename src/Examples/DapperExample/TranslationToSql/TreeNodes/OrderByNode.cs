using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class OrderByNode : SqlTreeNode
{
    public IReadOnlyList<OrderByTermNode> Terms { get; }

    public OrderByNode(IReadOnlyList<OrderByTermNode> terms)
    {
        ArgumentGuard.NotNullNorEmpty(terms);

        Terms = terms;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitOrderBy(this, argument);
    }
}
