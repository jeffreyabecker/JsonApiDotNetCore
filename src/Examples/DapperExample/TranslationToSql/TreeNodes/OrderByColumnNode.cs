using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class OrderByColumnNode : OrderByTermNode
{
    public ColumnNode Column { get; }

    public OrderByColumnNode(ColumnNode column, bool isAscending)
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
