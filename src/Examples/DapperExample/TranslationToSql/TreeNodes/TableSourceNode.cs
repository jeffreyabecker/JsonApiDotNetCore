using JsonApiDotNetCore;
using JsonApiDotNetCore.Resources;

namespace DapperExample.TranslationToSql.TreeNodes;

internal abstract class TableSourceNode : SqlTreeNode
{
    public const string IdColumnName = nameof(Identifiable<object>.Id);

    public abstract IReadOnlyList<ColumnNode> AllColumns { get; }
    public abstract IReadOnlyList<ColumnNode> ScalarColumns { get; }
    public abstract IReadOnlyList<ColumnNode> ForeignKeyColumns { get; }

    public string? Alias { get; }

    protected TableSourceNode(string? alias)
    {
        Alias = alias;
    }

    public abstract TableSourceNode Clone(string? alias);

    public ColumnNode GetIdColumn(string? tableAlias)
    {
        return GetColumnByUnderlyingTableColumnName(ScalarColumns, IdColumnName, tableAlias);
    }

    // TODO: Redesign these for improved handling of sub-query push down.

    public ColumnNode GetColumn(string columnName, string? tableAlias)
    {
        ArgumentGuard.NotNullNorEmpty(columnName);

        return GetColumnByUnderlyingTableColumnName(AllColumns, columnName, tableAlias);
    }

    public ColumnNode? FindScalarColumn(string columnName, string? tableAlias)
    {
        ArgumentGuard.NotNullNorEmpty(columnName);

        return FindColumnByUnderlyingTableColumnName(ScalarColumns, columnName, tableAlias);
    }

    public ColumnNode GetForeignKeyColumn(string columnName, string? tableAlias)
    {
        ArgumentGuard.NotNullNorEmpty(columnName);

        return GetColumnByUnderlyingTableColumnName(ForeignKeyColumns, columnName, tableAlias);
    }

    protected abstract ColumnNode? FindColumnByUnderlyingTableColumnName(IEnumerable<ColumnNode> source, string columnName, string? tableAlias);

    private ColumnNode GetColumnByUnderlyingTableColumnName(IEnumerable<ColumnNode> source, string columnName, string? tableAlias)
    {
        ColumnNode? column = FindColumnByUnderlyingTableColumnName(source, columnName, tableAlias);

        if (column == null)
        {
            throw new InvalidOperationException($"Column '{columnName}' not found.");
        }

        return column;
    }
}
