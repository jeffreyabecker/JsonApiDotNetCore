using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

/// <summary>
/// Represents a DELETE FROM clause. For example: DELETE FROM Customers WHERE Id = @p1
/// </summary>
internal sealed class DeleteNode : SqlTreeNode
{
    public TableNode Table { get; }
    public FilterNode Where { get; }

    public DeleteNode(TableNode table, FilterNode where)
    {
        ArgumentGuard.NotNull(table);
        ArgumentGuard.NotNull(where);

        Table = table;
        Where = where;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitDelete(this, argument);
    }
}
