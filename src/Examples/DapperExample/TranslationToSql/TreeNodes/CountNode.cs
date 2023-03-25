using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

/// <summary>
/// Represents a count on the number of rows returned from a sub-query. For example, "(SELECT COUNT(*) FROM ...)" in: WHERE (SELECT COUNT(*) FROM ...) > @p1
/// </summary>
internal sealed class CountNode : SqlValueNode
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
