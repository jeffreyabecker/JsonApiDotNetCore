using JsonApiDotNetCore;
using JsonApiDotNetCore.Queries.Expressions;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class ComparisonNode : FilterNode
{
    public ComparisonOperator Operator { get; }
    public SqlTreeNode Left { get; }
    public SqlTreeNode Right { get; }

    public ComparisonNode(ComparisonOperator @operator, SqlTreeNode left, SqlTreeNode right)
    {
        ArgumentGuard.NotNull(left);
        ArgumentGuard.NotNull(right);

        Operator = @operator;
        Left = left;
        Right = right;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitComparison(this, argument);
    }
}
