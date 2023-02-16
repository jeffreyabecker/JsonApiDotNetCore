using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

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
