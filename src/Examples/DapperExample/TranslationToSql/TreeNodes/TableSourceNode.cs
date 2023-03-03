using JsonApiDotNetCore;
using JsonApiDotNetCore.Resources;

namespace DapperExample.TranslationToSql.TreeNodes;

internal abstract class TableSourceNode : SqlTreeNode
{
    public abstract IReadOnlyList<ColumnNode> AllColumns { get; }
    public abstract IReadOnlyList<ColumnNode> ScalarColumns { get; }
    public abstract IReadOnlyList<ColumnNode> ForeignKeyColumns { get; }

    public string? Alias { get; }

    protected TableSourceNode(string? alias)
    {
        Alias = alias;
    }

    public ColumnNode GetIdColumn()
    {
        return ScalarColumns.First(column => column.Name == nameof(Identifiable<object>.Id));
    }

    public ColumnNode GetColumn(string columnName)
    {
        ArgumentGuard.NotNullNorEmpty(columnName);

        ColumnNode? column = AllColumns.FirstOrDefault(column1 => column1.Name == columnName);

        if (column == null)
        {
            throw new InvalidOperationException($"Column '{columnName}' not found.");
        }

        return column;
    }

    public ColumnNode? FindScalarColumn(string columnName)
    {
        ArgumentGuard.NotNullNorEmpty(columnName);

        return ScalarColumns.FirstOrDefault(column => column.Name == columnName);
    }

    public ColumnNode GetForeignKeyColumn(string columnName)
    {
        ArgumentGuard.NotNullNorEmpty(columnName);

        ColumnNode? column = ForeignKeyColumns.FirstOrDefault(column => column.Name == columnName);

        if (column == null)
        {
            throw new InvalidOperationException($"Foreign key column '{columnName}' not found.");
        }

        return column;
    }
}
