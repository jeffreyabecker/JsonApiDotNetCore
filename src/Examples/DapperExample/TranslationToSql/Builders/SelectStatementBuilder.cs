using System.Net;
using DapperExample.Repositories;
using DapperExample.TranslationToSql.DataModel;
using DapperExample.TranslationToSql.TreeNodes;
using JsonApiDotNetCore;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Errors;
using JsonApiDotNetCore.Queries;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Resources.Annotations;
using JsonApiDotNetCore.Serialization.Objects;

namespace DapperExample.TranslationToSql.Builders;

internal sealed class SelectStatementBuilder : QueryExpressionVisitor<TableAccessorNode, SqlTreeNode>
{
    private readonly IDataModelService _dataModelService;
    private readonly TableAliasGenerator _tableAliasGenerator;
    private readonly ParameterGenerator _parameterGenerator;
    private readonly Dictionary<TableAccessorNode, Dictionary<RelationshipAttribute, TableAccessorNode>> _relatedTables = new();
    private readonly Dictionary<string, string> _oldToNewTableAliasMap = new();
    private readonly Dictionary<string, TableAccessorNode> _tablesByAlias = new();
    private readonly Dictionary<TableAccessorNode, IReadOnlyList<SelectorNode>> _selectorsPerTable = new();
    private readonly HashSet<string> _selectorNamesUsed = new();
    private readonly List<FilterNode> _whereConditions = new();
    private readonly List<OrderByTermNode> _orderByTerms = new();
    private SelectShape _selectShape;
    private LimitOffsetNode? _limitOffset;

    public SelectStatementBuilder(IDataModelService dataModelService)
        : this(dataModelService, new TableAliasGenerator(), new ParameterGenerator())
    {
        ArgumentGuard.NotNull(dataModelService);
    }

    private SelectStatementBuilder(IDataModelService dataModelService, TableAliasGenerator tableAliasGenerator, ParameterGenerator parameterGenerator)
    {
        _dataModelService = dataModelService;
        _tableAliasGenerator = tableAliasGenerator;
        _parameterGenerator = parameterGenerator;
    }

    private SelectStatementBuilder(SelectStatementBuilder source)
        : this(source._dataModelService, source._tableAliasGenerator, source._parameterGenerator)
    {
        _relatedTables = new Dictionary<TableAccessorNode, Dictionary<RelationshipAttribute, TableAccessorNode>>(source._relatedTables);
        _oldToNewTableAliasMap = source._oldToNewTableAliasMap;
        _tablesByAlias = source._tablesByAlias;
        _selectorsPerTable = new Dictionary<TableAccessorNode, IReadOnlyList<SelectorNode>>(source._selectorsPerTable);
        _selectorNamesUsed = new HashSet<string>(source._selectorNamesUsed);
        _whereConditions.AddRange(source._whereConditions);
        _orderByTerms.AddRange(source._orderByTerms);
        _selectShape = source._selectShape;
        _limitOffset = source._limitOffset;
    }

    public SelectNode Build(QueryLayer queryLayer, SelectShape selectShape)
    {
        ArgumentGuard.NotNull(queryLayer);

        var includeConverter = new QueryLayerIncludeConverter(queryLayer);
        includeConverter.ConvertIncludesToSelections();

        ResetState(selectShape, false);

        TableAccessorNode tableAccessor = CreatePrimaryTable(queryLayer.ResourceType);
        ConvertQueryLayer(queryLayer, tableAccessor);

        return ToSelectNode(true, null, false);
    }

    private void ResetState(SelectShape selectShape, bool isSubQuery)
    {
        _relatedTables.Clear();
        _selectorsPerTable.Clear();
        _selectorNamesUsed.Clear();
        _whereConditions.Clear();
        _orderByTerms.Clear();
        _selectShape = selectShape;
        _limitOffset = null;

        if (!isSubQuery)
        {
            _tableAliasGenerator.Reset();
            _parameterGenerator.Reset();

            _oldToNewTableAliasMap.Clear();
            _tablesByAlias.Clear();
        }
    }

    private SelectNode ToSelectNode(bool aliasSelectorsToTableColumnNames, string? alias, bool requireUniqueColumnNames)
    {
        FilterNode? where = GetWhere();
        OrderByNode? orderBy = !_orderByTerms.Any() ? null : new OrderByNode(_orderByTerms);

        Dictionary<TableAccessorNode, IReadOnlyList<SelectorNode>> selectorsPerTable =
            aliasSelectorsToTableColumnNames ? AliasSelectorsToTableColumnNames(_selectorsPerTable) : _selectorsPerTable;

        var selectNode = new SelectNode(selectorsPerTable, where, orderBy, _limitOffset, alias);

        if (requireUniqueColumnNames)
        {
            AssertUniqueColumnNames(selectNode);
        }

        return selectNode;
    }

    private static void AssertUniqueColumnNames(SelectNode selectNode)
    {
        string? firstDuplicate = selectNode.Columns.GroupBy(column => column.Name).Where(group => group.Count() > 1).Select(group => group.Key)
            .FirstOrDefault();

        if (firstDuplicate != null)
        {
            throw new InvalidOperationException($"Found duplicate column '{firstDuplicate}' in select.");
        }
    }

    private void PushDownIntoSubQuery()
    {
        string[] oldTableAliases = _selectorsPerTable.Keys.Select(tableAccessor => tableAccessor.Source.Alias).Cast<string>().ToArray();

        var subSelectBuilder = new SelectStatementBuilder(this);

        var selectorsToKeep = new Dictionary<TableAccessorNode, IReadOnlyList<SelectorNode>>(subSelectBuilder._selectorsPerTable);
        subSelectBuilder.SelectAllColumnsInAllTables(selectorsToKeep.Keys);

        var subQuery = subSelectBuilder.ToSelectNode(false, null, true);

        ResetState(_selectShape, true);

        TableAccessorNode outerTableAccessor = CreatePrimaryTable(subQuery);
        var aliasedSubQuery = (SelectNode)outerTableAccessor.Source;

        MapOldTableAliasesToSubQuery(aliasedSubQuery.Alias!, oldTableAliases);
        _selectorsPerTable[outerTableAccessor] = MapSelectorsFromSubQuery(selectorsToKeep.SelectMany(selector => selector.Value), aliasedSubQuery);
        _orderByTerms.AddRange(MapOrderByFromSubQuery(aliasedSubQuery));
    }

    private void MapOldTableAliasesToSubQuery(string newTableAlias, IEnumerable<string> oldTableAliases)
    {
        foreach (string oldTableAlias in oldTableAliases)
        {
            _oldToNewTableAliasMap[oldTableAlias] = newTableAlias;
        }
    }

    private void SelectAllColumnsInAllTables(IEnumerable<TableAccessorNode> tableAccessors)
    {
        _selectorsPerTable.Clear();
        _selectorNamesUsed.Clear();

        foreach (TableAccessorNode tableAccessor in tableAccessors)
        {
            SetColumnSelectors(tableAccessor, tableAccessor.Source.Columns);
        }
    }

    private IReadOnlyList<SelectorNode> MapSelectorsFromSubQuery(IEnumerable<SelectorNode> innerSelectorsToKeep, SelectNode select)
    {
        List<ColumnNode> outerColumnsToKeep = new();

        foreach (SelectorNode innerSelector in innerSelectorsToKeep)
        {
            if (innerSelector is ColumnSelectorNode innerColumnSelector)
            {
                // t2."Id" AS Id0 => t3.Id0
                ColumnNode innerColumn = innerColumnSelector.Column;
                ColumnNode outerColumn = select.Columns.Single(outerColumn => outerColumn.Selector.Column == innerColumn);
                outerColumnsToKeep.Add(outerColumn);
            }
            else
            {
                // TODO: If there's an alias, we should use it. Otherwise we could fallback to ordinal selector.
                throw new NotImplementedException("Mapping non-column selectors is not implemented.");
            }
        }

        return PreserveColumnOrderEnsuringUniqueNames(outerColumnsToKeep);
    }

    private static IEnumerable<OrderByTermNode> MapOrderByFromSubQuery(SelectNode select)
    {
        if (select.OrderBy != null)
        {
            foreach (OrderByTermNode term in select.OrderBy.Terms)
            {
                if (term is OrderByColumnNode orderByColumn)
                {
                    ColumnNode outerColumn = select.Columns.Single(outerColumn => outerColumn.Selector.Column == orderByColumn.Column);
                    yield return new OrderByColumnNode(outerColumn, term.IsAscending);
                }
                else
                {
                    yield return term;
                }
            }
        }
    }

    private TableAccessorNode CreatePrimaryTable(ResourceType resourceType)
    {
        IReadOnlyDictionary<string, ResourceFieldAttribute?> columnMappings = _dataModelService.GetColumnMappings(resourceType);
        var table = new TableNode(resourceType, columnMappings, _tableAliasGenerator.GetNext());
        var from = new FromNode(table);

        TrackPrimaryTable(from);
        return from;
    }

    private TableAccessorNode CreatePrimaryTable(TableSourceNode tableSource)
    {
        TableSourceNode clone = tableSource.Clone(_tableAliasGenerator.GetNext());
        var from = new FromNode(clone);

        TrackPrimaryTable(from);
        return from;
    }

    private void TrackPrimaryTable(TableAccessorNode tableAccessor)
    {
        if (_relatedTables.Count > 0)
        {
            throw new InvalidOperationException("A primary table already exists.");
        }

        _relatedTables.Add(tableAccessor, new Dictionary<RelationshipAttribute, TableAccessorNode>());
        _tablesByAlias.Add(tableAccessor.Source.Alias!, tableAccessor);

        _selectorsPerTable[tableAccessor] = _selectShape switch
        {
            SelectShape.Columns => Array.Empty<SelectorNode>(),
            SelectShape.Count => new CountSelectorNode(null).AsArray(),
            _ => new OneSelectorNode(null).AsArray()
        };
    }

    private void ConvertQueryLayer(QueryLayer queryLayer, TableAccessorNode tableAccessor)
    {
        if (queryLayer.Filter != null)
        {
            var filter = (FilterNode)Visit(queryLayer.Filter, tableAccessor);
            _whereConditions.Add(filter);
        }

        if (queryLayer.Sort != null)
        {
            var orderBy = (OrderByNode)Visit(queryLayer.Sort, tableAccessor);
            _orderByTerms.AddRange(orderBy.Terms);
        }

        if (queryLayer.Pagination is { PageSize: not null })
        {
            // TODO: Assumption: The caller has ensured we'll never find more than one pagination in the query layer tree.
            _limitOffset ??= (LimitOffsetNode)Visit(queryLayer.Pagination, tableAccessor);
        }

        if (queryLayer.Selection != null)
        {
            foreach (ResourceType resourceType in queryLayer.Selection.GetResourceTypes())
            {
                FieldSelectors selectors = queryLayer.Selection.GetOrCreateSelectors(resourceType);
                ConvertFieldSelectors(selectors, tableAccessor);
            }
        }
    }

    private void ConvertFieldSelectors(FieldSelectors selectors, TableAccessorNode tableAccessor)
    {
        HashSet<ColumnNode> selectedColumns = new();
        Dictionary<RelationshipAttribute, QueryLayer> nextToOneLayers = new();
        Dictionary<RelationshipAttribute, QueryLayer> nextToManyLayers = new();

        string? oldTableAlias = tableAccessor.Source.Alias;
        TableAccessorNode currentAccessor = ResolveMappedTableAccessor(oldTableAlias);

        if (selectors.ContainsReadOnlyAttribute || selectors.ContainsOnlyRelationships || selectors.IsEmpty)
        {
            // If a read-only attribute is selected, its calculated value likely depends on another property, so fetch all scalar properties.
            // And only selecting relationships implicitly means to fetch all scalar properties as well.
            // Additionally, empty selectors (originating from eliminated includes) indicate to fetch all scalar properties too.

            selectedColumns = currentAccessor.Source.Columns.Where(column => column.Type == ColumnType.Scalar).ToHashSet();
        }

        foreach ((ResourceFieldAttribute field, QueryLayer? nextLayer) in selectors.OrderBy(selector => selector.Key.PublicName))
        {
            if (field is AttrAttribute attribute)
            {
                ColumnNode? column = currentAccessor.Source.FindColumn(attribute.Property.Name, ColumnType.Scalar, oldTableAlias);

                if (column != null)
                {
                    selectedColumns.Add(column);
                }
            }

            if (field is RelationshipAttribute relationship && nextLayer != null)
            {
                if (relationship is HasOneAttribute)
                {
                    nextToOneLayers.Add(relationship, nextLayer);
                }
                else
                {
                    nextToManyLayers.Add(relationship, nextLayer);
                }
            }
        }

        if (_selectShape == SelectShape.Columns)
        {
            SetColumnSelectors(currentAccessor, selectedColumns);
        }

        foreach ((RelationshipAttribute relationship, QueryLayer nextLayer) in nextToOneLayers)
        {
            TableAccessorNode relatedTableAccessor = GetOrCreateRelatedTable(currentAccessor, relationship);
            ConvertQueryLayer(nextLayer, relatedTableAccessor);
        }

        foreach ((RelationshipAttribute relationship, QueryLayer nextLayer) in nextToManyLayers)
        {
            TableAccessorNode relatedTableAccessor = GetOrCreateRelatedTable(currentAccessor, relationship);
            ConvertQueryLayer(nextLayer, relatedTableAccessor);
        }
    }

    private TableAccessorNode GetOrCreateRelatedTable(TableAccessorNode leftTableAccessor, RelationshipAttribute relationship)
    {
        string? leftOldTableAlias = leftTableAccessor.Source.Alias;
        TableAccessorNode leftCurrentTableAccessor = ResolveMappedTableAccessor(leftOldTableAlias);

        TableAccessorNode? relatedTableAccessor = _relatedTables.Count == 0
            ? CreateFromWithIdentityCondition(leftCurrentTableAccessor, leftOldTableAlias, relationship)
            : FindRelatedTable(leftCurrentTableAccessor, relationship);

        if (relatedTableAccessor == null)
        {
            if (relationship is HasManyAttribute && _limitOffset != null)
            {
                PushDownIntoSubQuery();

                leftCurrentTableAccessor = ResolveMappedTableAccessor(leftOldTableAlias);
            }

            relatedTableAccessor = CreateJoin(leftCurrentTableAccessor, leftOldTableAlias, relationship);
            TrackRelatedTable(leftCurrentTableAccessor, relationship, relatedTableAccessor);
        }

        return relatedTableAccessor;
    }

    private TableAccessorNode? FindRelatedTable(TableAccessorNode leftTableAccessor, RelationshipAttribute relationship)
    {
        Dictionary<RelationshipAttribute, TableAccessorNode> rightTableAccessors = _relatedTables[leftTableAccessor];
        return rightTableAccessors.TryGetValue(relationship, out TableAccessorNode? rightTableAccessor) ? rightTableAccessor : null;
    }

    private void TrackRelatedTable(TableAccessorNode leftTableAccessor, RelationshipAttribute relationship, TableAccessorNode rightTableAccessor)
    {
        _relatedTables.Add(rightTableAccessor, new Dictionary<RelationshipAttribute, TableAccessorNode>());
        _tablesByAlias.Add(rightTableAccessor.Source.Alias!, rightTableAccessor);
        _selectorsPerTable[rightTableAccessor] = Array.Empty<SelectorNode>();

        _relatedTables[leftTableAccessor].Add(relationship, rightTableAccessor);
    }

    private TableAccessorNode CreateJoin(TableAccessorNode leftTableAccessor, string? leftOldTableAlias, RelationshipAttribute relationship)
    {
        RelationshipForeignKey foreignKey = _dataModelService.GetForeignKey(relationship);

        IReadOnlyDictionary<string, ResourceFieldAttribute?> columnMappings = _dataModelService.GetColumnMappings(relationship.RightType);
        var rightTable = new TableNode(relationship.RightType, columnMappings, _tableAliasGenerator.GetNext());

        ColumnNode rightColumn = foreignKey.IsAtLeftSide
            ? rightTable.GetIdColumn(rightTable.Alias)
            : rightTable.GetColumn(foreignKey.ColumnName, ColumnType.ForeignKey, rightTable.Alias);

        ColumnNode leftColumn = foreignKey.IsAtLeftSide
            ? leftTableAccessor.Source.GetColumn(foreignKey.ColumnName, ColumnType.ForeignKey, leftOldTableAlias)
            : leftTableAccessor.Source.GetIdColumn(leftOldTableAlias);

        return new JoinNode(foreignKey.IsNullable ? JoinType.LeftJoin : JoinType.InnerJoin, rightTable, rightColumn, leftColumn);
    }

    private TableAccessorNode ResolveMappedTableAccessor(string? tableAlias)
    {
        if (tableAlias == null)
        {
            throw new InvalidOperationException("Missing table alias.");
        }

        string currentTableAlias = tableAlias;

        while (_oldToNewTableAliasMap.ContainsKey(currentTableAlias))
        {
            currentTableAlias = _oldToNewTableAliasMap[currentTableAlias];
        }

        if (_tablesByAlias.TryGetValue(currentTableAlias, out TableAccessorNode? tableAccessor))
        {
            return tableAccessor;
        }

        throw new InvalidOperationException($"Table for alias '{tableAlias}' not found.");
    }

    private TableAccessorNode CreateFromWithIdentityCondition(TableAccessorNode outerTableAccessor, string? leftOldAlias, RelationshipAttribute relationship)
    {
        TableAccessorNode innerTableAccessor = CreatePrimaryTable(relationship.RightType);

        RelationshipForeignKey foreignKey = _dataModelService.GetForeignKey(relationship);

        ColumnNode innerColumn = foreignKey.IsAtLeftSide
            ? innerTableAccessor.Source.GetIdColumn(innerTableAccessor.Source.Alias)
            : innerTableAccessor.Source.GetColumn(foreignKey.ColumnName, ColumnType.ForeignKey, innerTableAccessor.Source.Alias);

        ColumnNode outerColumn = foreignKey.IsAtLeftSide
            ? outerTableAccessor.Source.GetColumn(foreignKey.ColumnName, ColumnType.ForeignKey, leftOldAlias)
            : outerTableAccessor.Source.GetIdColumn(leftOldAlias);

        var joinCondition = new ComparisonNode(ComparisonOperator.Equals, outerColumn, innerColumn);
        _whereConditions.Add(joinCondition);

        return innerTableAccessor;
    }

    private void SetColumnSelectors(TableAccessorNode tableAccessor, IEnumerable<ColumnNode> columns)
    {
        bool preserveOrder = tableAccessor.Source is SelectNode;

        if (preserveOrder)
        {
            _selectorsPerTable[tableAccessor] = PreserveColumnOrderEnsuringUniqueNames(columns);
        }
        else
        {
            _selectorsPerTable[tableAccessor] = OrderColumnsWithIdAtFrontEnsuringUniqueNames(columns);
        }
    }

    private List<SelectorNode> PreserveColumnOrderEnsuringUniqueNames(IEnumerable<ColumnNode> columns)
    {
        List<SelectorNode> selectors = new();

        foreach (ColumnNode column in columns)
        {
            string uniqueName = GetUniqueName(column.Name);
            string? selectorAlias = uniqueName != column.Name ? uniqueName : null;
            var columnSelector = new ColumnSelectorNode(column, selectorAlias);
            selectors.Add(columnSelector);
        }

        return selectors;
    }

    private List<SelectorNode> OrderColumnsWithIdAtFrontEnsuringUniqueNames(IEnumerable<ColumnNode> columns)
    {
        Dictionary<string, List<SelectorNode>> selectorsPerTable = new();

        foreach (ColumnNode column in columns.OrderBy(column => column.GetTableAliasIndex()).ThenBy(column => column.Name))
        {
            string tableAlias = column.TableAlias ?? "!";
            selectorsPerTable.TryAdd(tableAlias, new List<SelectorNode>());

            string uniqueName = GetUniqueName(column.Name);
            string? selectorAlias = uniqueName != column.Name ? uniqueName : null;
            var columnSelector = new ColumnSelectorNode(column, selectorAlias);

            if (column.Name == TableSourceNode.IdColumnName)
            {
                selectorsPerTable[tableAlias].Insert(0, columnSelector);
            }
            else
            {
                selectorsPerTable[tableAlias].Add(columnSelector);
            }
        }

        return selectorsPerTable.SelectMany(selector => selector.Value).ToList();
    }

    private string GetUniqueName(string columnName)
    {
        string uniqueName = columnName;

        while (_selectorNamesUsed.Contains(uniqueName))
        {
            uniqueName += "0";
        }

        _selectorNamesUsed.Add(uniqueName);
        return uniqueName;
    }

    private FilterNode? GetWhere()
    {
        if (_whereConditions.Count == 0)
        {
            return null;
        }

        if (_whereConditions.Count == 1)
        {
            return _whereConditions[0];
        }

        List<FilterNode> andTerms = new();

        foreach (FilterNode filter in _whereConditions.ToArray())
        {
            if (filter is LogicalNode { Operator: LogicalOperator.And } nestedAnd)
            {
                andTerms.AddRange(nestedAnd.Terms);
            }
            else
            {
                andTerms.Add(filter);
            }
        }

        return new LogicalNode(LogicalOperator.And, andTerms);
    }

    private static Dictionary<TableAccessorNode, IReadOnlyList<SelectorNode>> AliasSelectorsToTableColumnNames(
        Dictionary<TableAccessorNode, IReadOnlyList<SelectorNode>> selectorsPerTable)
    {
        // TODO: Remove unreferenced selectors in sub-queries.

        Dictionary<TableAccessorNode, IReadOnlyList<SelectorNode>> aliasedSelectors = new();

        foreach ((TableAccessorNode tableAccessor, IReadOnlyList<SelectorNode> tableSelectors) in selectorsPerTable)
        {
            aliasedSelectors[tableAccessor] = tableSelectors.Select(AliasToTableColumnName).ToList();
        }

        return aliasedSelectors;
    }

    private static SelectorNode AliasToTableColumnName(SelectorNode selector)
    {
        if (selector is ColumnSelectorNode columnSelector)
        {
            if (columnSelector.Column is ColumnInSelectNode columnInSelect)
            {
                string persistedColumnName = columnInSelect.GetPersistedColumnName();

                if (columnInSelect.Name != persistedColumnName)
                {
                    // t1.Id0 => t1.Id0 AS Id
                    return new ColumnSelectorNode(columnInSelect, persistedColumnName);
                }
            }

            if (columnSelector.Alias != null)
            {
                // t1."Id" AS Id0 => t1."Id"
                return new ColumnSelectorNode(columnSelector.Column, null);
            }
        }

        return selector;
    }

    public override SqlTreeNode DefaultVisit(QueryExpression expression, TableAccessorNode tableAccessor)
    {
        throw new NotSupportedException($"Expressions of type '{expression.GetType().Name}' are not supported.");
    }

    public override SqlTreeNode VisitComparison(ComparisonExpression expression, TableAccessorNode tableAccessor)
    {
        SqlValueNode left = VisitComparisonTerm(expression.Left, tableAccessor);
        SqlValueNode right = VisitComparisonTerm(expression.Right, tableAccessor);

        return new ComparisonNode(expression.Operator, left, right);
    }

    private SqlValueNode VisitComparisonTerm(QueryExpression comparisonTerm, TableAccessorNode tableAccessor)
    {
        if (comparisonTerm is NullConstantExpression)
        {
            return NullConstantNode.Instance;
        }

        SqlTreeNode treeNode = Visit(comparisonTerm, tableAccessor);

        if (treeNode is JoinNode join)
        {
            return join.JoinColumn;
        }

        return (SqlValueNode)treeNode;
    }

    public override SqlTreeNode VisitResourceFieldChain(ResourceFieldChainExpression expression, TableAccessorNode tableAccessor)
    {
        TableAccessorNode currentAccessor = tableAccessor;

        foreach (ResourceFieldAttribute field in expression.Fields)
        {
            if (field is RelationshipAttribute relationship)
            {
                currentAccessor = GetOrCreateRelatedTable(currentAccessor, relationship);
            }
            else if (field is AttrAttribute attribute)
            {
                string? oldTableAlias = currentAccessor.Source.Alias;
                currentAccessor = ResolveMappedTableAccessor(oldTableAlias);

                ColumnNode? column = currentAccessor.Source.FindColumn(attribute.Property.Name, ColumnType.Scalar, oldTableAlias);

                if (column == null)
                {
                    throw new JsonApiException(new ErrorObject(HttpStatusCode.BadRequest)
                    {
                        Title = "Sorting or filtering on the requested attribute is unavailable.",
                        Detail = $"Sorting or filtering on attribute '{attribute.PublicName}' is unavailable."
                    });
                }

                return column;
            }
        }

        return currentAccessor;
    }

    public override SqlTreeNode VisitLiteralConstant(LiteralConstantExpression expression, TableAccessorNode tableAccessor)
    {
        return _parameterGenerator.Create(expression.TypedValue);
    }

    public override SqlTreeNode VisitNullConstant(NullConstantExpression expression, TableAccessorNode tableAccessor)
    {
        return _parameterGenerator.Create(null);
    }

    public override SqlTreeNode VisitLogical(LogicalExpression expression, TableAccessorNode tableAccessor)
    {
        FilterNode[] terms = VisitSequence<FilterExpression, FilterNode>(expression.Terms, tableAccessor).ToArray();
        return new LogicalNode(expression.Operator, terms);
    }

    private IEnumerable<TOut> VisitSequence<TIn, TOut>(IEnumerable<TIn> source, TableAccessorNode tableAccessor)
        where TIn : QueryExpression
        where TOut : SqlTreeNode
    {
        return source.Select(expression => (TOut)Visit(expression, tableAccessor)).ToList();
    }

    public override SqlTreeNode VisitNot(NotExpression expression, TableAccessorNode tableAccessor)
    {
        var child = (FilterNode)Visit(expression.Child, tableAccessor);
        return new NotNode(child);
    }

    public override SqlTreeNode VisitHas(HasExpression expression, TableAccessorNode tableAccessor)
    {
        var subSelectBuilder = new SelectStatementBuilder(this);
        subSelectBuilder.ResetState(SelectShape.One, true);

        return subSelectBuilder.GetExistsClause(expression, tableAccessor);
    }

    private ExistsNode GetExistsClause(HasExpression expression, TableAccessorNode outerTableAccessor)
    {
        var rightTableAccessor = (TableAccessorNode)Visit(expression.TargetCollection, outerTableAccessor);

        if (expression.Filter != null)
        {
            var filter = (FilterNode)Visit(expression.Filter, rightTableAccessor);
            _whereConditions.Add(filter);
        }

        SelectNode select = ToSelectNode(false, null, false);
        return new ExistsNode(select);
    }

    public override SqlTreeNode VisitIsType(IsTypeExpression expression, TableAccessorNode tableAccessor)
    {
        throw new NotSupportedException("Resource inheritance is not supported.");
    }

    public override SqlTreeNode VisitSortElement(SortElementExpression expression, TableAccessorNode tableAccessor)
    {
        if (expression.Count != null)
        {
            var count = (CountNode)Visit(expression.Count, tableAccessor);
            return new OrderByCountNode(count, expression.IsAscending);
        }

        if (expression.TargetAttribute != null)
        {
            var column = (ColumnNode)Visit(expression.TargetAttribute, tableAccessor);
            return new OrderByColumnNode(column, expression.IsAscending);
        }

        throw new InvalidOperationException("Internal error: Unreachable code detected.");
    }

    public override SqlTreeNode VisitSort(SortExpression expression, TableAccessorNode tableAccessor)
    {
        OrderByTermNode[] terms = VisitSequence<SortElementExpression, OrderByTermNode>(expression.Elements, tableAccessor).ToArray();
        return new OrderByNode(terms);
    }

    public override SqlTreeNode VisitPagination(PaginationExpression expression, TableAccessorNode tableAccessor)
    {
        ParameterNode limitParameter = _parameterGenerator.Create(expression.PageSize!.Value);

        ParameterNode? offsetParameter = !expression.PageNumber.Equals(PageNumber.ValueOne)
            ? _parameterGenerator.Create(expression.PageSize.Value * (expression.PageNumber.OneBasedValue - 1))
            : null;

        return new LimitOffsetNode(limitParameter, offsetParameter);
    }

    public override SqlTreeNode VisitCount(CountExpression expression, TableAccessorNode tableAccessor)
    {
        var subSelectBuilder = new SelectStatementBuilder(this);
        subSelectBuilder.ResetState(SelectShape.Count, true);

        return subSelectBuilder.GetCountClause(expression, tableAccessor);
    }

    private CountNode GetCountClause(CountExpression expression, TableAccessorNode outerTableAccessor)
    {
        _ = Visit(expression.TargetCollection, outerTableAccessor);

        SelectNode select = ToSelectNode(false, null, false);
        return new CountNode(select);
    }

    public override SqlTreeNode VisitMatchText(MatchTextExpression expression, TableAccessorNode tableAccessor)
    {
        var column = (ColumnNode)Visit(expression.TargetAttribute, tableAccessor);
        return new LikeNode(column, expression.MatchKind, (string)expression.TextValue.TypedValue);
    }

    public override SqlTreeNode VisitAny(AnyExpression expression, TableAccessorNode tableAccessor)
    {
        var column = (ColumnNode)Visit(expression.TargetAttribute, tableAccessor);

        ParameterNode[] parameters =
            VisitSequence<LiteralConstantExpression, ParameterNode>(expression.Constants.OrderBy(constant => constant.TypedValue), tableAccessor).ToArray();

        return parameters.Length == 1 ? new ComparisonNode(ComparisonOperator.Equals, column, parameters[0]) : new InNode(column, parameters);
    }
}
