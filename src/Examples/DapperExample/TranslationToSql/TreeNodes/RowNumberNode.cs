using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class RowNumberNode : SelectorNode
{
    // https://www.sqlservertutorial.net/sql-server-window-functions/sql-server-row_number-function/

    public ColumnNode? PartitionBy { get; }
    public OrderByNode OrderBy { get; }

    public RowNumberNode(ColumnNode? partitionBy, OrderByNode orderBy, string? alias)
        : base(alias)
    {
        ArgumentGuard.NotNull(orderBy);

        PartitionBy = partitionBy;
        OrderBy = orderBy;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitRowNumber(this, argument);
    }
}
