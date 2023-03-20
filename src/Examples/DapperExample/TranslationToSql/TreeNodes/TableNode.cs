using Humanizer;
using JsonApiDotNetCore;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Resources.Annotations;

namespace DapperExample.TranslationToSql.TreeNodes;

// TODO: Replace read-only collections with immutable collections.

internal sealed class TableNode : TableSourceNode
{
    private readonly ResourceType _resourceType;
    private readonly IReadOnlyDictionary<string, ResourceFieldAttribute?> _columnMappings;
    private readonly List<ColumnInTableNode> _columns = new();

    public string Name => _resourceType.ClrType.Name.Pluralize();

    public override IReadOnlyList<ColumnInTableNode> Columns => _columns;

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
            ColumnType columnType = field is RelationshipAttribute ? ColumnType.ForeignKey : ColumnType.Scalar;
            var column = new ColumnInTableNode(columnName, columnType, Alias);

            _columns.Add(column);
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
