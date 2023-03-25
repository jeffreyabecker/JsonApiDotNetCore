using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

/// <summary>
/// Represents an INSERT INTO clause. For example: INSERT INTO Customers (FirstName, LastName) VALUES @p1, @p2
/// </summary>
internal sealed class InsertNode : SqlTreeNode
{
    public TableNode Table { get; }
    public IReadOnlyCollection<ColumnAssignmentNode> Assignments { get; }

    public InsertNode(TableNode table, IReadOnlyCollection<ColumnAssignmentNode> assignments)
    {
        ArgumentGuard.NotNull(table);
        ArgumentGuard.NotNullNorEmpty(assignments);

        Table = table;
        Assignments = assignments;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitInsert(this, argument);
    }
}
