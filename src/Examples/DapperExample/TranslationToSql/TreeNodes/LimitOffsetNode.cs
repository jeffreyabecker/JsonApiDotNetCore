using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

/// <summary>
/// Represents the LIMIT and OFFSET clauses, which are used to constrain the number of returned rows. For example: LIMIT @p1 OFFSET @p2
/// </summary>
internal sealed class LimitOffsetNode : SqlTreeNode
{
    public ParameterNode Limit { get; }
    public ParameterNode? Offset { get; }

    public LimitOffsetNode(ParameterNode limit, ParameterNode? offset)
    {
        ArgumentGuard.NotNull(limit);

        Limit = limit;
        Offset = offset;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitLimitOffset(this, argument);
    }
}
