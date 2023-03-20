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

internal sealed class SelectStatementBuilder : QueryExpressionVisitor<TableAccessorReference, SqlTreeNode>
{
    private readonly IDataModelService _dataModelService;
    private readonly TableAliasGenerator _tableAliasGenerator;
    private readonly ParameterGenerator _parameterGenerator;
    private readonly Dictionary<TableAccessorReference, Dictionary<RelationshipAttribute, TableAccessorReference>> _relatedTables = new();
    private readonly Dictionary<string, string> _oldToNewTableAliasMap = new();
    private readonly Dictionary<string, TableSourceNode> _tablesByAlias = new();
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
        _relatedTables = new Dictionary<TableAccessorReference, Dictionary<RelationshipAttribute, TableAccessorReference>>(source._relatedTables);
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

        ResetState(selectShape, true);

        TableAccessorReference tableReference = CreateFrom(queryLayer.ResourceType);
        ConvertQueryLayer(queryLayer, tableReference);

        return ToSelectNode(true, null, false);
    }

    private void ResetState(SelectShape selectShape, bool resetGenerators)
    {
        _relatedTables.Clear();
        _selectorsPerTable.Clear();
        _selectorNamesUsed.Clear();
        _whereConditions.Clear();
        _orderByTerms.Clear();
        _selectShape = selectShape;
        _limitOffset = null;

        if (resetGenerators)
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
        string[] oldTableAliases = _selectorsPerTable.Keys.Select(tableAccessor => tableAccessor.TableSource.Alias).Cast<string>().ToArray();

        var subSelectBuilder = new SelectStatementBuilder(this);

        var selectorsToKeep = new Dictionary<TableAccessorNode, IReadOnlyList<SelectorNode>>(subSelectBuilder._selectorsPerTable);
        subSelectBuilder.SelectAllColumnsInAllTables(selectorsToKeep.Keys);

        var subQuery = subSelectBuilder.ToSelectNode(false, null, true);

        ResetState(_selectShape, false);

        TableAccessorReference outerTableReference = CreateFrom(subQuery);
        var aliasedSubQuery = (SelectNode)outerTableReference.Value.TableSource;

        MapOldTableAliases(aliasedSubQuery.Alias!, oldTableAliases);
        _tablesByAlias[aliasedSubQuery.Alias!] = aliasedSubQuery;
        _selectorsPerTable[outerTableReference.Value] = MapSelectorsFromSubQuery(selectorsToKeep.SelectMany(selector => selector.Value), aliasedSubQuery);
        _orderByTerms.AddRange(MapOrderByFromSubQuery(aliasedSubQuery));
    }

    private void MapOldTableAliases(string newTableAlias, IEnumerable<string> oldTableAliases)
    {
        foreach (string oldTableAlias in oldTableAliases)
        {
            _oldToNewTableAliasMap[oldTableAlias] = newTableAlias;
        }
    }

    private string ResolveMappedTableAlias(string tableAlias)
    {
        string current = tableAlias;

        while (_oldToNewTableAliasMap.ContainsKey(current))
        {
            current = _oldToNewTableAliasMap[current];
        }

        return current;
    }

    private void SelectAllColumnsInAllTables(IEnumerable<TableAccessorNode> tableAccessors)
    {
        _selectorsPerTable.Clear();
        _selectorNamesUsed.Clear();

        foreach (TableAccessorNode tableAccessor in tableAccessors)
        {
            SetColumnSelectors(tableAccessor, tableAccessor.TableSource.Columns);
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

    private TableAccessorReference CreateFrom(ResourceType resourceType)
    {
        IReadOnlyDictionary<string, ResourceFieldAttribute?> columnMappings = _dataModelService.GetColumnMappings(resourceType);
        var table = new TableNode(resourceType, columnMappings, _tableAliasGenerator.GetNext());
        var from = new FromNode(table);
        var reference = new TableAccessorReference(from);

        IncludePrimaryTable(reference);
        return reference;
    }

    private TableAccessorReference CreateFrom(TableSourceNode tableSource)
    {
        TableSourceNode clone = tableSource.Clone(_tableAliasGenerator.GetNext());
        var from = new FromNode(clone);
        var reference = new TableAccessorReference(from);

        IncludePrimaryTable(reference);
        return reference;
    }

    private void IncludePrimaryTable(TableAccessorReference tableReference)
    {
        _relatedTables[tableReference] = new Dictionary<RelationshipAttribute, TableAccessorReference>();
        _tablesByAlias[tableReference.Value.TableSource.Alias!] = tableReference.Value.TableSource;

        _selectorsPerTable[tableReference.Value] = _selectShape switch
        {
            SelectShape.Columns => Array.Empty<SelectorNode>(),
            SelectShape.Count => new CountSelectorNode(null).AsArray(),
            _ => new OneSelectorNode(null).AsArray()
        };
    }

    private void ConvertQueryLayer(QueryLayer queryLayer, TableAccessorReference tableReference)
    {
        if (queryLayer.Filter != null)
        {
            var filter = (FilterNode)Visit(queryLayer.Filter, tableReference);
            _whereConditions.Add(filter);
        }

        if (queryLayer.Sort != null)
        {
            var orderBy = (OrderByNode)Visit(queryLayer.Sort, tableReference);
            _orderByTerms.AddRange(orderBy.Terms);
        }

        if (queryLayer.Pagination is { PageSize: not null })
        {
            // TODO: Assumption: The caller has ensured we'll never find more than one pagination in the query layer tree.
            _limitOffset ??= (LimitOffsetNode)Visit(queryLayer.Pagination, tableReference);
        }

        if (queryLayer.Selection != null)
        {
            foreach (ResourceType resourceType in queryLayer.Selection.GetResourceTypes())
            {
                FieldSelectors selectors = queryLayer.Selection.GetOrCreateSelectors(resourceType);
                ConvertFieldSelectors(selectors, tableReference);
            }
        }
    }

    private void ConvertFieldSelectors(FieldSelectors selectors, TableAccessorReference tableReference)
    {
        HashSet<ColumnNode> selectedColumns = new();
        Dictionary<RelationshipAttribute, QueryLayer> nextToOneLayers = new();
        Dictionary<RelationshipAttribute, QueryLayer> nextToManyLayers = new();

        if (selectors.ContainsReadOnlyAttribute || selectors.ContainsOnlyRelationships || selectors.IsEmpty)
        {
            // If a read-only attribute is selected, its calculated value likely depends on another property, so fetch all scalar properties.
            // And only selecting relationships implicitly means to fetch all scalar properties as well.
            // Additionally, empty selectors (originating from eliminated includes) indicate to fetch all scalar properties too.

            selectedColumns = tableReference.Value.TableSource.Columns.Where(column => column.Type == ColumnType.Scalar).ToHashSet();
        }

        foreach ((ResourceFieldAttribute field, QueryLayer? nextLayer) in selectors.OrderBy(selector => selector.Key.PublicName))
        {
            if (field is AttrAttribute attribute)
            {
                string tableAlias = ResolveMappedTableAlias(tableReference.Value.TableSource.Alias!);
                ColumnNode? column = tableReference.Value.TableSource.FindColumn(attribute.Property.Name, ColumnType.Scalar, tableAlias);

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
            SetColumnSelectors(tableReference.Value, selectedColumns);
        }

        foreach ((RelationshipAttribute relationship, QueryLayer nextLayer) in nextToOneLayers)
        {
            TableAccessorReference relatedTableReference = GetOrCreateRelatedTable(tableReference, relationship);
            ConvertQueryLayer(nextLayer, relatedTableReference);
        }

        foreach ((RelationshipAttribute relationship, QueryLayer nextLayer) in nextToManyLayers)
        {
            TableAccessorReference relatedTableReference = GetOrCreateRelatedTable(tableReference, relationship);
            ConvertQueryLayer(nextLayer, relatedTableReference);
        }
    }

    private TableAccessorReference GetOrCreateRelatedTable(TableAccessorReference leftTableReference, RelationshipAttribute relationship)
    {
        TableAccessorReference? relatedTableReference = FindRelatedTable(leftTableReference, relationship);

        if (relatedTableReference == null)
        {
            if (_selectorsPerTable.Count == 0)
            {
                relatedTableReference = CreateFromWithIdentityCondition(leftTableReference.Value.TableSource, relationship);
            }
            else
            {
                if (relationship is HasManyAttribute && _limitOffset != null)
                {
                    PushDownIntoSubQuery();
                }

                relatedTableReference = CreateJoin(leftTableReference.Value.TableSource, relationship);
                IncludeRelatedTable(leftTableReference, relationship, relatedTableReference);
            }
        }

        return relatedTableReference;
    }

    private TableAccessorReference? FindRelatedTable(TableAccessorReference leftTableReference, RelationshipAttribute relationship)
    {
        if (_relatedTables.TryGetValue(leftTableReference, out Dictionary<RelationshipAttribute, TableAccessorReference>? rightTableReferences))
        {
            if (rightTableReferences.TryGetValue(relationship, out TableAccessorReference? rightTableReference))
            {
                return rightTableReference;
            }
        }

        return null;
    }

    private void IncludeRelatedTable(TableAccessorReference leftTableReference, RelationshipAttribute relationship, TableAccessorReference rightTableReference)
    {
        _relatedTables.TryAdd(leftTableReference, new Dictionary<RelationshipAttribute, TableAccessorReference>());
        _tablesByAlias.TryAdd(leftTableReference.Value.TableSource.Alias!, leftTableReference.Value.TableSource);
        _relatedTables[leftTableReference].Add(relationship, rightTableReference);

        _relatedTables[rightTableReference] = new Dictionary<RelationshipAttribute, TableAccessorReference>();
        _tablesByAlias[rightTableReference.Value.TableSource.Alias!] = rightTableReference.Value.TableSource;
        _selectorsPerTable[rightTableReference.Value] = Array.Empty<SelectorNode>();
    }

    private TableAccessorReference CreateJoin(TableSourceNode leftTableSource, RelationshipAttribute relationship)
    {
        RelationshipForeignKey foreignKey = _dataModelService.GetForeignKey(relationship);

        IReadOnlyDictionary<string, ResourceFieldAttribute?> columnMappings = _dataModelService.GetColumnMappings(relationship.RightType);
        var rightTable = new TableNode(relationship.RightType, columnMappings, _tableAliasGenerator.GetNext());

        ColumnNode rightColumn = foreignKey.IsAtLeftSide
            ? rightTable.GetIdColumn(rightTable.Alias)
            : rightTable.GetColumn(foreignKey.ColumnName, ColumnType.ForeignKey, rightTable.Alias);

        TableSourceNode leftTable = ResolveMappedTableSource(leftTableSource.Alias);

        ColumnNode leftColumn = foreignKey.IsAtLeftSide
            ? leftTable.GetColumn(foreignKey.ColumnName, ColumnType.ForeignKey, leftTableSource.Alias)
            : leftTable.GetIdColumn(leftTableSource.Alias);

        var join = new JoinNode(foreignKey.IsNullable ? JoinType.LeftJoin : JoinType.InnerJoin, rightTable, rightColumn, leftColumn);
        return new TableAccessorReference(join);
    }

    private TableSourceNode ResolveMappedTableSource(string? tableAlias)
    {
        if (tableAlias == null)
        {
            throw new InvalidOperationException("Missing table alias.");
        }

        string currentTableAlias = ResolveMappedTableAlias(tableAlias);

        if (_tablesByAlias.TryGetValue(currentTableAlias, out TableSourceNode? tableSource))
        {
            return tableSource;
        }

        throw new InvalidOperationException("Table source not found.");
    }

    private TableAccessorReference CreateFromWithIdentityCondition(TableSourceNode outerTableSource, RelationshipAttribute relationship)
    {
        TableAccessorReference innerTableReference = CreateFrom(relationship.RightType);
        TableSourceNode innerTable = innerTableReference.Value.TableSource;

        RelationshipForeignKey foreignKey = _dataModelService.GetForeignKey(relationship);

        ColumnNode innerColumn = foreignKey.IsAtLeftSide
            ? innerTable.GetIdColumn(innerTable.Alias)
            : innerTable.GetColumn(foreignKey.ColumnName, ColumnType.ForeignKey, innerTable.Alias);

        TableSourceNode outerTable = ResolveMappedTableSource(outerTableSource.Alias);

        ColumnNode outerColumn = foreignKey.IsAtLeftSide
            ? outerTable.GetColumn(foreignKey.ColumnName, ColumnType.ForeignKey, outerTableSource.Alias)
            : outerTable.GetIdColumn(outerTableSource.Alias);

        var joinCondition = new ComparisonNode(ComparisonOperator.Equals, outerColumn, innerColumn);
        _whereConditions.Add(joinCondition);

        return innerTableReference;
    }

    private void SetColumnSelectors(TableAccessorNode tableAccessor, IEnumerable<ColumnNode> columns)
    {
        bool preserveOrder = tableAccessor.TableSource is SelectNode;

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

        foreach ((TableAccessorNode tableReference, IReadOnlyList<SelectorNode> tableSelectors) in selectorsPerTable)
        {
            aliasedSelectors[tableReference] = tableSelectors.Select(AliasToTableColumnName).ToList();
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

    public override SqlTreeNode DefaultVisit(QueryExpression expression, TableAccessorReference tableReference)
    {
        throw new NotSupportedException($"Expressions of type '{expression.GetType().Name}' are not supported.");
    }

    public override SqlTreeNode VisitComparison(ComparisonExpression expression, TableAccessorReference tableReference)
    {
        SqlValueNode left = VisitComparisonTerm(expression.Left, tableReference);
        SqlValueNode right = VisitComparisonTerm(expression.Right, tableReference);

        return new ComparisonNode(expression.Operator, left, right);
    }

    private SqlValueNode VisitComparisonTerm(QueryExpression comparisonTerm, TableAccessorReference tableReference)
    {
        if (comparisonTerm is NullConstantExpression)
        {
            return NullConstantNode.Instance;
        }

        SqlTreeNode treeNode = Visit(comparisonTerm, tableReference);

        if (treeNode is TableAccessorReference { Value: JoinNode join })
        {
            return join.JoinColumn;
        }

        return (SqlValueNode)treeNode;
    }

    public override SqlTreeNode VisitResourceFieldChain(ResourceFieldChainExpression expression, TableAccessorReference tableReference)
    {
        TableAccessorReference currentTableReference = tableReference;

        foreach (ResourceFieldAttribute field in expression.Fields)
        {
            if (field is RelationshipAttribute relationship)
            {
                currentTableReference = GetOrCreateRelatedTable(currentTableReference, relationship);
            }
            else if (field is AttrAttribute attribute)
            {
                string tableAlias = ResolveMappedTableAlias(currentTableReference.Value.TableSource.Alias!);
                ColumnNode? column = currentTableReference.Value.TableSource.FindColumn(attribute.Property.Name, ColumnType.Scalar, tableAlias);

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

        return currentTableReference;
    }

    public override SqlTreeNode VisitLiteralConstant(LiteralConstantExpression expression, TableAccessorReference tableReference)
    {
        return _parameterGenerator.Create(expression.TypedValue);
    }

    public override SqlTreeNode VisitNullConstant(NullConstantExpression expression, TableAccessorReference tableReference)
    {
        return _parameterGenerator.Create(null);
    }

    public override SqlTreeNode VisitLogical(LogicalExpression expression, TableAccessorReference tableReference)
    {
        FilterNode[] terms = VisitSequence<FilterExpression, FilterNode>(expression.Terms, tableReference).ToArray();
        return new LogicalNode(expression.Operator, terms);
    }

    private IEnumerable<TOut> VisitSequence<TIn, TOut>(IEnumerable<TIn> source, TableAccessorReference tableReference)
        where TIn : QueryExpression
        where TOut : SqlTreeNode
    {
        return source.Select(expression => (TOut)Visit(expression, tableReference)).ToList();
    }

    public override SqlTreeNode VisitNot(NotExpression expression, TableAccessorReference tableReference)
    {
        var child = (FilterNode)Visit(expression.Child, tableReference);
        return new NotNode(child);
    }

    public override SqlTreeNode VisitHas(HasExpression expression, TableAccessorReference tableReference)
    {
        var subSelectBuilder = new SelectStatementBuilder(this);
        subSelectBuilder.ResetState(SelectShape.One, false);

        return subSelectBuilder.GetExistsClause(expression, tableReference);
    }

    private ExistsNode GetExistsClause(HasExpression expression, TableAccessorReference outerTableReference)
    {
        var rightTableReference = (TableAccessorReference)Visit(expression.TargetCollection, outerTableReference);

        if (expression.Filter != null)
        {
            var filter = (FilterNode)Visit(expression.Filter, rightTableReference);
            _whereConditions.Add(filter);
        }

        SelectNode select = ToSelectNode(false, null, false);
        return new ExistsNode(select);
    }

    public override SqlTreeNode VisitIsType(IsTypeExpression expression, TableAccessorReference tableReference)
    {
        throw new NotSupportedException("Resource inheritance is not supported.");
    }

    public override SqlTreeNode VisitSortElement(SortElementExpression expression, TableAccessorReference tableReference)
    {
        if (expression.Count != null)
        {
            var count = (CountNode)Visit(expression.Count, tableReference);
            return new OrderByCountNode(count, expression.IsAscending);
        }

        if (expression.TargetAttribute != null)
        {
            var column = (ColumnNode)Visit(expression.TargetAttribute, tableReference);
            return new OrderByColumnNode(column, expression.IsAscending);
        }

        throw new InvalidOperationException("Internal error: Unreachable code detected.");
    }

    public override SqlTreeNode VisitSort(SortExpression expression, TableAccessorReference tableReference)
    {
        OrderByTermNode[] terms = VisitSequence<SortElementExpression, OrderByTermNode>(expression.Elements, tableReference).ToArray();
        return new OrderByNode(terms);
    }

    public override SqlTreeNode VisitPagination(PaginationExpression expression, TableAccessorReference tableReference)
    {
        ParameterNode limitParameter = _parameterGenerator.Create(expression.PageSize!.Value);

        ParameterNode? offsetParameter = !expression.PageNumber.Equals(PageNumber.ValueOne)
            ? _parameterGenerator.Create(expression.PageSize.Value * (expression.PageNumber.OneBasedValue - 1))
            : null;

        return new LimitOffsetNode(limitParameter, offsetParameter);
    }

    public override SqlTreeNode VisitCount(CountExpression expression, TableAccessorReference tableReference)
    {
        var subSelectBuilder = new SelectStatementBuilder(this);
        subSelectBuilder.ResetState(SelectShape.Count, false);

        return subSelectBuilder.GetCountClause(expression, tableReference);
    }

    private CountNode GetCountClause(CountExpression expression, TableAccessorReference outerTableReference)
    {
        _ = Visit(expression.TargetCollection, outerTableReference);

        SelectNode select = ToSelectNode(false, null, false);
        return new CountNode(select);
    }

    public override SqlTreeNode VisitMatchText(MatchTextExpression expression, TableAccessorReference tableReference)
    {
        var column = (ColumnNode)Visit(expression.TargetAttribute, tableReference);
        return new LikeNode(column, expression.MatchKind, (string)expression.TextValue.TypedValue);
    }

    public override SqlTreeNode VisitAny(AnyExpression expression, TableAccessorReference tableReference)
    {
        var column = (ColumnNode)Visit(expression.TargetAttribute, tableReference);

        ParameterNode[] parameters =
            VisitSequence<LiteralConstantExpression, ParameterNode>(expression.Constants.OrderBy(constant => constant.TypedValue), tableReference).ToArray();

        return parameters.Length == 1 ? new ComparisonNode(ComparisonOperator.Equals, column, parameters[0]) : new InNode(column, parameters);
    }
}
