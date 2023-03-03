using Humanizer;
using JsonApiDotNetCore;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Resources.Annotations;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class TableNode : TableSourceNode
{
    private readonly List<ColumnNode> _allColumns = new();
    private readonly List<ColumnNode> _scalarColumns = new();
    private readonly List<ColumnNode> _foreignKeyColumns = new();

    public ResourceType ResourceType { get; }
    public string Name => ResourceType.ClrType.Name.Pluralize();

    public override IReadOnlyList<ColumnNode> AllColumns => _allColumns;
    public override IReadOnlyList<ColumnNode> ScalarColumns => _scalarColumns;
    public override IReadOnlyList<ColumnNode> ForeignKeyColumns => _foreignKeyColumns;

    public TableNode(ResourceType resourceType, IReadOnlyDictionary<string, ResourceFieldAttribute?> columnMappings, string? alias)
        : base(alias)
    {
        ArgumentGuard.NotNull(resourceType);
        ArgumentGuard.NotNull(columnMappings);

        ResourceType = resourceType;

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

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitTable(this, argument);
    }
}
