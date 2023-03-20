using JsonApiDotNetCore;
using JsonApiDotNetCore.Resources;

namespace DapperExample.TranslationToSql.TreeNodes;

internal abstract class TableSourceNode : SqlTreeNode
{
    public const string IdColumnName = nameof(Identifiable<object>.Id);

    public abstract IReadOnlyList<ColumnNode> Columns { get; }
    public string? Alias { get; }

    protected TableSourceNode(string? alias)
    {
        Alias = alias;
    }

    public abstract TableSourceNode Clone(string? alias);

    public ColumnNode GetIdColumn(string? tableAlias)
    {
        IEnumerable<ColumnNode> scalarColumns = GetScalarColumns();
        return GetColumnByUnderlyingTableColumnName(scalarColumns, IdColumnName, tableAlias);
    }

    // TODO: Redesign these for efficiency and improved handling of sub-query push down.

    public IEnumerable<ColumnNode> GetScalarColumns()
    {
        return Columns.Where(column => column.Type == ColumnType.Scalar);
    }

    public ColumnNode GetColumn(string columnName, string? tableAlias)
    {
        ArgumentGuard.NotNullNorEmpty(columnName);

        return GetColumnByUnderlyingTableColumnName(Columns, columnName, tableAlias);
    }

    public ColumnNode? FindScalarColumn(string columnName, string? tableAlias)
    {
        ArgumentGuard.NotNullNorEmpty(columnName);

        IEnumerable<ColumnNode> scalarColumns = GetScalarColumns();
        return FindColumnByUnderlyingTableColumnName(scalarColumns, columnName, tableAlias);
    }

    public ColumnNode GetForeignKeyColumn(string columnName, string? tableAlias)
    {
        ArgumentGuard.NotNullNorEmpty(columnName);

        IEnumerable<ColumnNode> foreignKeyColumns = Columns.Where(column => column.Type == ColumnType.ForeignKey);
        return GetColumnByUnderlyingTableColumnName(foreignKeyColumns, columnName, tableAlias);
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
