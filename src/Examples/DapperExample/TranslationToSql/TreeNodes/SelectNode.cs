using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class SelectNode : TableSourceNode
{
    private readonly List<ColumnNode> _allColumns = new();
    private readonly List<ColumnNode> _scalarColumns = new();
    private readonly List<ColumnNode> _foreignKeyColumns = new();

    public IReadOnlyDictionary<TableAccessorNode, IReadOnlyList<SelectorNode>> Selectors { get; }
    public FilterNode? Where { get; }
    public OrderByNode? OrderBy { get; }
    public LimitOffsetNode? LimitOffset { get; }

    public override IReadOnlyList<ColumnNode> AllColumns => _allColumns;
    public override IReadOnlyList<ColumnNode> ScalarColumns => _scalarColumns;
    public override IReadOnlyList<ColumnNode> ForeignKeyColumns => _foreignKeyColumns;

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
        foreach ((TableAccessorNode tableAccessor, IReadOnlyList<SelectorNode> selectorsInTable) in selectors)
        {
            ReadTableSelectors(tableAccessor, selectorsInTable);
        }
    }

    private void ReadTableSelectors(TableAccessorNode tableAccessor, IReadOnlyList<SelectorNode> selectors)
    {
        foreach (ColumnSelectorNode columnSelector in selectors.OfType<ColumnSelectorNode>())
        {
            string innerName = columnSelector.Alias ?? columnSelector.Column.Name;
            var column = new ColumnNode(innerName, Alias);

            _allColumns.Add(column);

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

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitSelect(this, argument);
    }
}
