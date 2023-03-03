using Humanizer;
using JsonApiDotNetCore;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class TableNode : SqlTreeNode
{
    private readonly List<ColumnNode> _allColumns = new();
    private readonly List<ColumnNode> _scalarColumns = new();
    private readonly List<ColumnNode> _foreignKeyColumns = new();

    public ResourceType ResourceType { get; }
    public string Name => ResourceType.ClrType.Name.Pluralize();
    public string? Alias { get; }

    public IReadOnlyList<ColumnNode> AllColumns => _allColumns;
    public IReadOnlyList<ColumnNode> ScalarColumns => _scalarColumns;
    public IReadOnlyList<ColumnNode> ForeignKeyColumns => _foreignKeyColumns;

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
            var column = new ColumnNode(columnName, Alias);
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

    public ColumnNode GetIdColumn()
    {
        return ScalarColumns.First(column => column.Name == nameof(Identifiable<object>.Id));
    }

    public ColumnNode? FindColumn(string columnName)
    {
        ArgumentGuard.NotNullNorEmpty(columnName);

        return AllColumns.FirstOrDefault(column => column.Name == columnName);
    }

    public ColumnNode GetColumn(string columnName)
    {
        ArgumentGuard.NotNullNorEmpty(columnName);

        ColumnNode? column = FindColumn(columnName);

        if (column == null)
        {
            throw new InvalidOperationException($"Column '{columnName}' does not belong to table '{Name}'.");
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
            throw new InvalidOperationException($"Foreign key column '{columnName}' does not belong to table '{Name}'.");
        }

        return column;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitTable(this, argument);
    }
}
