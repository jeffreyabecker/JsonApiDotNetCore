namespace DapperExample.TranslationToSql.TreeNodes;

/// <summary>
/// Represents a reference to a column in a <see cref="TableNode"/>/ For example, "t1.FirstName" in: FROM Users AS t1
/// </summary>
internal sealed class ColumnInTableNode : ColumnNode
{
    public ColumnInTableNode(string name, ColumnType type, string? tableAlias)
        : base(name, type, tableAlias)
    {
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitColumnInTable(this, argument);
    }
}
