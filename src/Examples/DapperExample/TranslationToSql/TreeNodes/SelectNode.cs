using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class SelectNode : TableSourceNode
{
    private readonly List<ColumnInSelectNode> _allColumns = new();
    private readonly List<ColumnInSelectNode> _scalarColumns = new();
    private readonly List<ColumnInSelectNode> _foreignKeyColumns = new();

    public IReadOnlyDictionary<TableAccessorNode, IReadOnlyList<SelectorNode>> Selectors { get; }
    public FilterNode? Where { get; }
    public OrderByNode? OrderBy { get; }
    public LimitOffsetNode? LimitOffset { get; }

    public override IReadOnlyList<ColumnInSelectNode> AllColumns => _allColumns;
    public override IReadOnlyList<ColumnInSelectNode> ScalarColumns => _scalarColumns;
    public override IReadOnlyList<ColumnInSelectNode> ForeignKeyColumns => _foreignKeyColumns;

    public SelectNode(IReadOnlyDictionary<TableAccessorNode, IReadOnlyList<SelectorNode>> selectors, FilterNode? where, OrderByNode? orderBy,
        LimitOffsetNode? limitOffset, string? alias)
        : base(alias)
    {
        ArgumentGuard.NotNullNorEmpty(selectors);

        Selectors = selectors;
        Where = where;
        OrderBy = orderBy;
        LimitOffset = limitOffset;

        ReadSelectorColumns(selectors);
    }

    private void ReadSelectorColumns(IReadOnlyDictionary<TableAccessorNode, IReadOnlyList<SelectorNode>> selectors)
    {
        foreach ((TableAccessorNode tableAccessor, IReadOnlyList<SelectorNode> tableSelectors) in selectors)
        {
            ReadTableSelectors(tableAccessor, tableSelectors);
        }
    }

    private void ReadTableSelectors(TableAccessorNode tableAccessor, IEnumerable<SelectorNode> tableSelectors)
    {
        foreach (ColumnSelectorNode columnSelector in tableSelectors.OfType<ColumnSelectorNode>())
        {
            var column = new ColumnInSelectNode(columnSelector, Alias);
            _allColumns.Add(column);

            // TODO: Verify this resolves properly when selectors are aliased.
            bool isForeignKeyColumn = tableAccessor.TableSource.ForeignKeyColumns.Any(nextColumn => nextColumn.Name == columnSelector.Column.Name);

            if (isForeignKeyColumn)
            {
                _foreignKeyColumns.Add(column);
            }

            bool isScalarColumn = tableAccessor.TableSource.ScalarColumns.Any(nextColumn => nextColumn.Name == columnSelector.Column.Name);

            if (isScalarColumn)
            {
                _scalarColumns.Add(column);
            }
        }
    }

    public override TableSourceNode Clone(string? alias)
    {
        return new SelectNode(Selectors, Where, OrderBy, LimitOffset, alias);
    }

    protected override ColumnNode? FindColumnByUnderlyingTableColumnName(IEnumerable<ColumnNode> source, string columnName, string? tableAlias)
    {
        foreach (ColumnInSelectNode column in source.OfType<ColumnInSelectNode>().Where(column => column.Selector.Column.TableAlias == tableAlias))
        {
            string underlyingTableColumnName = column.GetUnderlyingTableColumnName();

            if (underlyingTableColumnName == columnName)
            {
                return column;
            }
        }

        return null;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitSelect(this, argument);
    }
}
