using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class TableColumnNode : SqlValueNode
{
    public TableNode Table { get; }
    public string Name { get; }

    public TableColumnNode(TableNode table, string name)
    {
        ArgumentGuard.NotNull(table);
        ArgumentGuard.NotNullNorEmpty(name);

        Table = table;
        Name = name;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitTableColumn(this, argument);
    }
}
