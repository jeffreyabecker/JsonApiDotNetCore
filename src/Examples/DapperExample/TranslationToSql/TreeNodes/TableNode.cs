using Humanizer;
using JsonApiDotNetCore;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Resources.Annotations;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class TableNode : TableSourceNode
{
    private readonly ResourceType _resourceType;
    private readonly IReadOnlyDictionary<string, ResourceFieldAttribute?> _columnMappings;
    private readonly List<ColumnNode> _allColumns = new();
    private readonly List<ColumnNode> _scalarColumns = new();
    private readonly List<ColumnNode> _foreignKeyColumns = new();

    public string Name => _resourceType.ClrType.Name.Pluralize();

    public override IReadOnlyList<ColumnNode> AllColumns => _allColumns;
    public override IReadOnlyList<ColumnNode> ScalarColumns => _scalarColumns;
    public override IReadOnlyList<ColumnNode> ForeignKeyColumns => _foreignKeyColumns;

    public TableNode(ResourceType resourceType, IReadOnlyDictionary<string, ResourceFieldAttribute?> columnMappings, string? alias)
        : base(alias)
    {
        ArgumentGuard.NotNull(resourceType);
        ArgumentGuard.NotNull(columnMappings);

        _resourceType = resourceType;

        _columnMappings = columnMappings;
        ReadColumnMappings();
    }

    public override TableSourceNode Clone(string? alias)
    {
        return new TableNode(_resourceType, _columnMappings, alias);
    }

    private void ReadColumnMappings()
    {
        foreach ((string columnName, ResourceFieldAttribute? field) in _columnMappings)
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

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitTable(this, argument);
    }
}
