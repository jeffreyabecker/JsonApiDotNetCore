using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class SelectNode : TableSourceNode
{
    private readonly List<ColumnInSelectNode> _columns = new();

    public IReadOnlyDictionary<TableAccessorNode, IReadOnlyList<SelectorNode>> Selectors { get; }
    public FilterNode? Where { get; }
    public OrderByNode? OrderBy { get; }
    public LimitOffsetNode? LimitOffset { get; }

    public override IReadOnlyList<ColumnInSelectNode> Columns => _columns;

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
        foreach (ColumnSelectorNode columnSelector in selectors.SelectMany(selector => selector.Value).OfType<ColumnSelectorNode>())
        {
            var column = new ColumnInSelectNode(columnSelector, Alias);
            _columns.Add(column);
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
