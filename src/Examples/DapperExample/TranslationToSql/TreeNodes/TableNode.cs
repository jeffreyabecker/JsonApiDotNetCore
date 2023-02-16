using Humanizer;
using JsonApiDotNetCore;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class TableNode : SqlTreeNode
{
    private readonly List<TableColumnNode> _allColumns = new();
    private readonly List<TableColumnNode> _scalarColumns = new();
    private readonly List<TableColumnNode> _foreignKeyColumns = new();

    public ResourceType ResourceType { get; }
    public string Name => ResourceType.ClrType.Name.Pluralize();
    public string? Alias { get; }

    public IReadOnlyList<TableColumnNode> AllColumns => _allColumns;
    public IReadOnlyList<TableColumnNode> ScalarColumns => _scalarColumns;
    public IReadOnlyList<TableColumnNode> ForeignKeyColumns => _foreignKeyColumns;

    public TableNode(ResourceType resourceType, string? alias, IReadOnlyDictionary<string, ResourceFieldAttribute?> columnMappings)
    {
        ArgumentGuard.NotNull(resourceType);
        ArgumentGuard.NotNull(columnMappings);

        ResourceType = resourceType;
        Alias = alias;

        ReadColumnMappings(columnMappings);
    }

    private void ReadColumnMappings(IReadOnlyDictionary<string, ResourceFieldAttribute?> columnMappings)
    {
        foreach ((string columnName, ResourceFieldAttribute? field) in columnMappings)
        {
            var column = new TableColumnNode(this, columnName);
            _allColumns.Add(column);

            if (field is RelationshipAttribute)
            {
                _foreignKeyColumns.Add(column);
            }
            else
            {
                _scalarColumns.Add(column);
            }
        }
    }

    public TableColumnNode GetIdColumn()
    {
        return ScalarColumns.First(column => column.Name == nameof(Identifiable<object>.Id));
    }

    public TableColumnNode? FindColumn(string columnName)
    {
        ArgumentGuard.NotNullNorEmpty(columnName);

        return AllColumns.FirstOrDefault(column => column.Name == columnName);
    }

    public TableColumnNode GetColumn(string columnName)
    {
        ArgumentGuard.NotNullNorEmpty(columnName);

        TableColumnNode? column = FindColumn(columnName);

        if (column == null)
        {
            throw new InvalidOperationException($"Column '{columnName}' does not belong to table '{Name}'.");
        }

        return column;
    }

    public TableColumnNode? FindScalarColumn(string columnName)
    {
        ArgumentGuard.NotNullNorEmpty(columnName);

        return ScalarColumns.FirstOrDefault(column => column.Name == columnName);
    }

    public TableColumnNode GetForeignKeyColumn(string columnName)
    {
        ArgumentGuard.NotNullNorEmpty(columnName);

        TableColumnNode? column = ForeignKeyColumns.FirstOrDefault(column => column.Name == columnName);

        if (column == null)
        {
            throw new InvalidOperationException($"Foreign key column '{columnName}' does not belong to table '{Name}'.");
        }

        return column;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitTable(this, argument);
    }
}
