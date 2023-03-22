using System.Diagnostics.CodeAnalysis;
using DapperExample.TranslationToSql.TreeNodes;
using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql;

internal sealed class StaleColumnReferenceRewriter : SqlTreeNodeVisitor<ColumnVisitMode, SqlTreeNode>
{
    private readonly IReadOnlyDictionary<string, string> _oldToNewTableAliasMap;
    private readonly ILogger<StaleColumnReferenceRewriter> _logger;
    private readonly Stack<Dictionary<string, TableSourceNode>> _tablesInScopeStack = new();

    public StaleColumnReferenceRewriter(IReadOnlyDictionary<string, string> oldToNewTableAliasMap, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(oldToNewTableAliasMap);
        ArgumentGuard.NotNull(loggerFactory);

        _oldToNewTableAliasMap = oldToNewTableAliasMap;
        _logger = loggerFactory.CreateLogger<StaleColumnReferenceRewriter>();
    }

    public SelectNode PullColumnsIntoScope(SelectNode select)
    {
        _tablesInScopeStack.Clear();

        return TypedVisit(select, ColumnVisitMode.Reference);
    }

    public override SqlTreeNode DefaultVisit(SqlTreeNode node, ColumnVisitMode mode)
    {
        throw new NotSupportedException($"Nodes of type '{node.GetType().Name}' are not supported.");
    }

    public override SqlTreeNode VisitSelect(SelectNode node, ColumnVisitMode mode)
    {
        IncludeTableAliasInCurrentScope(node);

        using IDisposable scope = EnterSelectScope();

        IReadOnlyDictionary<TableAccessorNode, IReadOnlyList<SelectorNode>> selectors = VisitSelectors(node.Selectors, mode);
        FilterNode? where = TypedVisit(node.Where, mode);
        OrderByNode? orderBy = TypedVisit(node.OrderBy, mode);
        LimitOffsetNode? limitOffset = TypedVisit(node.LimitOffset, mode);
        return new SelectNode(selectors, where, orderBy, limitOffset, node.Alias);
    }

    private void IncludeTableAliasInCurrentScope(TableSourceNode tableSource)
    {
        if (tableSource.Alias != null)
        {
            Dictionary<string, TableSourceNode> tablesInScope = _tablesInScopeStack.Peek();
            tablesInScope.Add(tableSource.Alias, tableSource);
        }
    }

    private IDisposable EnterSelectScope()
    {
        Dictionary<string, TableSourceNode> newScope = CopyTopStackElement();
        _tablesInScopeStack.Push(newScope);

        return new PopStackOnDispose<Dictionary<string, TableSourceNode>>(_tablesInScopeStack);
    }

    private Dictionary<string, TableSourceNode> CopyTopStackElement()
    {
        if (_tablesInScopeStack.Count == 0)
        {
            return new Dictionary<string, TableSourceNode>();
        }

        Dictionary<string, TableSourceNode> topElement = _tablesInScopeStack.Peek();
        return new Dictionary<string, TableSourceNode>(topElement);
    }

    private IReadOnlyDictionary<TableAccessorNode, IReadOnlyList<SelectorNode>> VisitSelectors(
        IReadOnlyDictionary<TableAccessorNode, IReadOnlyList<SelectorNode>> selectors, ColumnVisitMode mode)
    {
        Dictionary<TableAccessorNode, IReadOnlyList<SelectorNode>> newSelectors = new();

        foreach ((TableAccessorNode tableAccessor, IReadOnlyList<SelectorNode> tableSelectors) in selectors)
        {
            TableAccessorNode newTableAccessor = TypedVisit(tableAccessor, mode);
            IReadOnlyList<SelectorNode> newTableSelectors = VisitList(tableSelectors, ColumnVisitMode.Declaration);

            newSelectors.Add(newTableAccessor, newTableSelectors);
        }

        return newSelectors;
    }

    public override SqlTreeNode VisitTable(TableNode node, ColumnVisitMode mode)
    {
        IncludeTableAliasInCurrentScope(node);
        return node;
    }

    public override SqlTreeNode VisitFrom(FromNode node, ColumnVisitMode mode)
    {
        TableSourceNode source = TypedVisit(node.Source, mode);
        return new FromNode(source);
    }

    public override SqlTreeNode VisitJoin(JoinNode node, ColumnVisitMode mode)
    {
        TableSourceNode source = TypedVisit(node.Source, mode);
        ColumnNode outerColumn = TypedVisit(node.OuterColumn, mode);
        ColumnNode innerColumn = TypedVisit(node.InnerColumn, mode);
        return new JoinNode(node.JoinType, source, outerColumn, innerColumn);
    }

    public override SqlTreeNode VisitColumnInTable(ColumnInTableNode node, ColumnVisitMode mode)
    {
        if (mode == ColumnVisitMode.Declaration)
        {
            return node;
        }

        Dictionary<string, TableSourceNode> tablesInScope = _tablesInScopeStack.Peek();
        return MapColumnInTable(node, tablesInScope);
    }

    private ColumnNode MapColumnInTable(ColumnInTableNode column, IDictionary<string, TableSourceNode> tablesInScope)
    {
        if (column.TableAlias != null && !tablesInScope.ContainsKey(column.TableAlias))
        {
            // Stale column found. Keep pulling out until in scope.
            string currentAlias = column.TableAlias;

            while (_oldToNewTableAliasMap.ContainsKey(currentAlias))
            {
                currentAlias = _oldToNewTableAliasMap[currentAlias];

                if (tablesInScope.TryGetValue(currentAlias, out TableSourceNode? currentTable))
                {
                    ColumnNode? outerColumn = currentTable.FindColumn(column.Name, null, column.TableAlias);

                    if (outerColumn != null)
                    {
                        _logger.LogDebug($"Mapped inaccessible column {column} to {outerColumn}.");
                        return outerColumn;
                    }
                }
            }

            string candidateScopes = string.Join(", ", tablesInScope.Select(table => table.Key));
            throw new InvalidOperationException($"Failed to map inaccessible column {column} to any of the tables in scope: {candidateScopes}.");
        }

        return column;
    }

    public override SqlTreeNode VisitColumnInSelect(ColumnInSelectNode node, ColumnVisitMode mode)
    {
        if (mode == ColumnVisitMode.Declaration)
        {
            return node;
        }

        ColumnSelectorNode selector = TypedVisit(node.Selector, mode);
        return new ColumnInSelectNode(selector, node.TableAlias);
    }

    public override SqlTreeNode VisitColumnSelector(ColumnSelectorNode node, ColumnVisitMode mode)
    {
        ColumnNode column = TypedVisit(node.Column, mode);
        return new ColumnSelectorNode(column, node.Alias);
    }

    public override SqlTreeNode VisitOneSelector(OneSelectorNode node, ColumnVisitMode mode)
    {
        return node;
    }

    public override SqlTreeNode VisitCountSelector(CountSelectorNode node, ColumnVisitMode mode)
    {
        return node;
    }

    public override SqlTreeNode VisitNot(NotNode node, ColumnVisitMode mode)
    {
        FilterNode child = TypedVisit(node.Child, mode);
        return new NotNode(child);
    }

    public override SqlTreeNode VisitLogical(LogicalNode node, ColumnVisitMode mode)
    {
        IReadOnlyList<FilterNode> terms = VisitList(node.Terms, mode);
        return new LogicalNode(node.Operator, terms);
    }

    public override SqlTreeNode VisitComparison(ComparisonNode node, ColumnVisitMode mode)
    {
        SqlValueNode left = TypedVisit(node.Left, mode);
        SqlValueNode right = TypedVisit(node.Right, mode);
        return new ComparisonNode(node.Operator, left, right);
    }

    public override SqlTreeNode VisitLike(LikeNode node, ColumnVisitMode mode)
    {
        ColumnNode column = TypedVisit(node.Column, mode);
        return new LikeNode(column, node.MatchKind, node.Text);
    }

    public override SqlTreeNode VisitIn(InNode node, ColumnVisitMode mode)
    {
        ColumnNode column = TypedVisit(node.Column, mode);
        IReadOnlyList<SqlValueNode> values = VisitList(node.Values, mode);
        return new InNode(column, values);
    }

    public override SqlTreeNode VisitExists(ExistsNode node, ColumnVisitMode mode)
    {
        SelectNode subSelect = TypedVisit(node.SubSelect, mode);
        return new ExistsNode(subSelect);
    }

    public override SqlTreeNode VisitCount(CountNode node, ColumnVisitMode mode)
    {
        SelectNode subSelect = TypedVisit(node.SubSelect, mode);
        return new CountNode(subSelect);
    }

    public override SqlTreeNode VisitOrderBy(OrderByNode node, ColumnVisitMode mode)
    {
        IReadOnlyList<OrderByTermNode> terms = VisitList(node.Terms, mode);
        return new OrderByNode(terms);
    }

    public override SqlTreeNode VisitOrderByColumn(OrderByColumnNode node, ColumnVisitMode mode)
    {
        ColumnNode column = TypedVisit(node.Column, mode);
        return new OrderByColumnNode(column, node.IsAscending);
    }

    public override SqlTreeNode VisitOrderByCount(OrderByCountNode node, ColumnVisitMode mode)
    {
        CountNode count = TypedVisit(node.Count, mode);
        return new OrderByCountNode(count, node.IsAscending);
    }

    public override SqlTreeNode VisitLimitOffset(LimitOffsetNode node, ColumnVisitMode mode)
    {
        ParameterNode limit = TypedVisit(node.Limit, mode);
        ParameterNode? offset = TypedVisit(node.Offset, mode);
        return new LimitOffsetNode(limit, offset);
    }

    public override SqlTreeNode VisitParameter(ParameterNode node, ColumnVisitMode mode)
    {
        return node;
    }

    public override SqlTreeNode VisitNullConstant(NullConstantNode node, ColumnVisitMode mode)
    {
        return node;
    }

    [return: NotNullIfNotNull("element")]
    private T? TypedVisit<T>(T? element, ColumnVisitMode mode)
        where T : SqlTreeNode
    {
        return element != null ? (T)Visit(element, mode) : null;
    }

    private IReadOnlyList<T> VisitList<T>(IEnumerable<T> elements, ColumnVisitMode mode)
        where T : SqlTreeNode
    {
        return elements.Select(element => TypedVisit(element, mode)).ToList();
    }

    private sealed class PopStackOnDispose<T> : IDisposable
    {
        private readonly Stack<T> _stack;

        public PopStackOnDispose(Stack<T> stack)
        {
            _stack = stack;
        }

        public void Dispose()
        {
            _stack.Pop();
        }
    }
}
