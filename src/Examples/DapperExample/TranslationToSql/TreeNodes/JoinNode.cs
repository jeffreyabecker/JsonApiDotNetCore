using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

/// <summary>
/// Represents a JOIN clause. For example: LEFT JOIN Customers AS t2 ON t1.CustomerId = t2.Id
/// </summary>
internal sealed class JoinNode : TableAccessorNode
{
    public JoinType JoinType { get; }
    public ColumnNode OuterColumn { get; }
    public ColumnNode InnerColumn { get; }

    public JoinNode(JoinType joinType, TableSourceNode source, ColumnNode outerColumn, ColumnNode innerColumn)
        : base(source)
    {
        ArgumentGuard.NotNull(outerColumn);
        ArgumentGuard.NotNull(innerColumn);

        JoinType = joinType;
        OuterColumn = outerColumn;
        InnerColumn = innerColumn;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitJoin(this, argument);
    }
}
