using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class OrderByColumnNode : OrderByTermNode
{
    public TableColumnNode Column { get; }

    public OrderByColumnNode(TableColumnNode column, bool isAscending)
        : base(isAscending)
    {
        ArgumentGuard.NotNull(column);

        Column = column;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitOrderByColumn(this, argument);
    }
}
