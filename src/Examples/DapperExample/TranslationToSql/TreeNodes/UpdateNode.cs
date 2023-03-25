using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

/// <summary>
/// Represents an UPDATE clause. For example: UPDATE Customers SET FirstName = @p1 WHERE Id = @p2
/// </summary>
internal sealed class UpdateNode : SqlTreeNode
{
    public TableNode Table { get; }
    public IReadOnlyCollection<ColumnAssignmentNode> Assignments { get; }
    public FilterNode Where { get; }

    public UpdateNode(TableNode table, IReadOnlyCollection<ColumnAssignmentNode> assignments, FilterNode where)
    {
        ArgumentGuard.NotNull(table);
        ArgumentGuard.NotNullNorEmpty(assignments);
        ArgumentGuard.NotNull(where);

        Table = table;
        Assignments = assignments;
        Where = where;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitUpdate(this, argument);
    }
}
