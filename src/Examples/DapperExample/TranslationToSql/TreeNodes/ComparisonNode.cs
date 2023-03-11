using JsonApiDotNetCore;
using JsonApiDotNetCore.Queries.Expressions;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class ComparisonNode : FilterNode
{
    public ComparisonOperator Operator { get; }
    public SqlValueNode Left { get; }
    public SqlValueNode Right { get; }

    public ComparisonNode(ComparisonOperator @operator, SqlValueNode left, SqlValueNode right)
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