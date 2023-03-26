using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

/// <summary>
/// Represents a WHERE clause. For example: WHERE t1.Amount > @p1
/// </summary>
internal sealed class WhereNode : SqlTreeNode
{
    public FilterNode Filter { get; }

    public WhereNode(FilterNode filter)
    {
        ArgumentGuard.NotNull(filter);

        Filter = filter;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitWhere(this, argument);
    }
}
