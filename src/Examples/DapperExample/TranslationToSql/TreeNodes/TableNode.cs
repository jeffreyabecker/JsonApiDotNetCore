using Humanizer;
using JsonApiDotNetCore;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Resources.Annotations;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class TableNode : TableSourceNode
{
    private readonly ResourceType _resourceType;
    private readonly IReadOnlyDictionary<string, ResourceFieldAttribute?> _columnMappings;
    private readonly List<ColumnInTableNode> _allColumns = new();
    private readonly List<ColumnInTableNode> _scalarColumns = new();
    private readonly List<ColumnInTableNode> _foreignKeyColumns = new();

    public string Name => _resourceType.ClrType.Name.Pluralize();

    public override IReadOnlyList<ColumnInTableNode> AllColumns => _allColumns;
    public override IReadOnlyList<ColumnInTableNode> ScalarColumns => _scalarColumns;
    public override IReadOnlyList<ColumnInTableNode> ForeignKeyColumns => _foreignKeyColumns;

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
            var column = new ColumnInTableNode(columnName, Alias);
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

    protected override ColumnNode? FindColumnByUnderlyingTableColumnName(IEnumerable<ColumnNode> source, string columnName, string? tableAlias)
    {
        return tableAlias != Alias ? null : source.FirstOrDefault(column => column.Name == columnName);
    }
}
