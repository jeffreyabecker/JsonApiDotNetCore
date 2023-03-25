using System.Net;
using DapperExample.TranslationToSql.DataModel;
using DapperExample.TranslationToSql.Generators;
using DapperExample.TranslationToSql.Transformations;
using DapperExample.TranslationToSql.TreeNodes;
using JsonApiDotNetCore;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Errors;
using JsonApiDotNetCore.Queries;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Resources.Annotations;
using JsonApiDotNetCore.Serialization.Objects;

namespace DapperExample.TranslationToSql.Builders;

/// <summary>
/// Builds a SELECT statement from a <see cref="QueryLayer" />.
/// </summary>
internal sealed class SelectStatementBuilder : QueryExpressionVisitor<TableAccessorNode, SqlTreeNode>
{
    // State that is shared between sub-queries.
    private readonly QueryState _queryState;

    // The FROM/JOIN/sub-SELECT tables, along with their selectors (which usually are column references).
    private readonly Dictionary<TableAccessorNode, IReadOnlyList<SelectorNode>> _selectorsPerTable;

    // Used to assign unique names when adding selectors, in case tables are joined that would result in duplicate column names.
    private readonly HashSet<string> _selectorNamesUsed;

    // Filter constraints.
    private readonly List<FilterNode> _whereConditions;

    // Sorting on columns, or COUNT(*) in a sub-query.
    private readonly List<OrderByTermNode> _orderByTerms;

    // Indicates whether to select a set of columns, the number of rows, or only the first (unnamed) column.
    private SelectShape _selectShape;

    // Pagination, which can only occur once.
    private LimitOffsetNode? _limitOffset;

    // When filtering on "EXISTS (SELECT COUNT (*) FROM ...)" or "EXISTS (SELECT 1 FROM ...)", the sub-query must not use LEFT JOINs.
    private bool _forceInnerJoins;

    public SelectStatementBuilder(IDataModelService dataModelService, ILoggerFactory loggerFactory)
        : this(new QueryState(dataModelService, new TableAliasGenerator(), new ParameterGenerator(), loggerFactory))
    {
    }

    private SelectStatementBuilder(QueryState queryState)
    {
        _queryState = queryState;
        _selectorsPerTable = new Dictionary<TableAccessorNode, IReadOnlyList<SelectorNode>>();
        _selectorNamesUsed = new HashSet<string>();
        _whereConditions = new List<FilterNode>();
        _orderByTerms = new List<OrderByTermNode>();
    }

    private SelectStatementBuilder(SelectStatementBuilder source)
        : this(source._queryState)
    {
        // Copy constructor.
        _queryState = source._queryState;
        _selectorsPerTable = new Dictionary<TableAccessorNode, IReadOnlyList<SelectorNode>>(source._selectorsPerTable);
        _selectorNamesUsed = new HashSet<string>(source._selectorNamesUsed);
        _whereConditions.AddRange(source._whereConditions);
        _orderByTerms.AddRange(source._orderByTerms);
        _selectShape = source._selectShape;
        _limitOffset = source._limitOffset;
        _forceInnerJoins = source._forceInnerJoins;
    }

    public SelectNode Build(QueryLayer queryLayer, SelectShape selectShape)
    {
        ArgumentGuard.NotNull(queryLayer);

        // Convert queryLayer.Include into multiple levels of queryLayer.Selection.
        var includeConverter = new QueryLayerIncludeConverter(queryLayer);
        includeConverter.ConvertIncludesToSelections();

        ResetState(selectShape, false);

        TableAccessorNode primaryTableAccessor = CreatePrimaryTable(queryLayer.ResourceType);
        ConvertQueryLayer(queryLayer, primaryTableAccessor);

        SelectNode select = ToSelect(false);

        if (_selectShape == SelectShape.Columns && _queryState.HasPushDownOccurred)
        {
            var staleRewriter = new StaleColumnReferenceRewriter(_queryState.OldToNewTableAliasMap, _queryState.LoggerFactory);
            select = staleRewriter.PullColumnsIntoScope(select);

            var selectorsRewriter = new UnusedSelectorsRewriter(_queryState.LoggerFactory);
            select = selectorsRewriter.RemoveUnusedSelectorsInSubQueries(select);
        }

        return select;
    }

    private void ResetState(SelectShape selectShape, bool isSubQuery)
    {
        _selectorsPerTable.Clear();
        _selectorNamesUsed.Clear();
        _whereConditions.Clear();
        _orderByTerms.Clear();
        _selectShape = selectShape;
        _limitOffset = null;
        _forceInnerJoins = false;

        if (!isSubQuery)
        {
            _queryState.Reset();
        }
    }

    private TableAccessorNode CreatePrimaryTable(ResourceType resourceType)
    {
        IReadOnlyDictionary<string, ResourceFieldAttribute?> columnMappings = _queryState.DataModelService.GetColumnMappings(resourceType);
        var table = new TableNode(resourceType, columnMappings, _queryState.TableAliasGenerator.GetNext());
        var from = new FromNode(table);

        TrackPrimaryTable(from);
        return from;
    }

    private TableAccessorNode CreatePrimaryTable(TableSourceNode tableSource)
    {
        TableSourceNode clone = tableSource.Clone(_queryState.TableAliasGenerator.GetNext());
        var from = new FromNode(clone);

        TrackPrimaryTable(from);
        return from;
    }

    private void TrackPrimaryTable(TableAccessorNode tableAccessor)
    {
        if (_selectorsPerTable.Count > 0)
        {
            throw new InvalidOperationException("A primary table already exists.");
        }

        _queryState.RelatedTables.Add(tableAccessor, new Dictionary<RelationshipAttribute, TableAccessorNode>());

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
            if (_queryState.HasPagination)
            {
                // The caller should ensure we'll never find more than one pagination in the query layer tree.
                throw new NotSupportedException("Multiple levels of pagination are not supported.");
            }

            _limitOffset = (LimitOffsetNode)Visit(queryLayer.Pagination, tableAccessor);
            _queryState.HasPagination = true;
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
        Dictionary<RelationshipAttribute, QueryLayer> nextLayers = new();

        if (selectors.ContainsReadOnlyAttribute || selectors.ContainsOnlyRelationships || selectors.IsEmpty)
        {
            // If a read-only attribute is selected, its calculated value likely depends on another property, so fetch all scalar properties.
            // And only selecting relationships implicitly means to fetch all scalar properties as well.
            // Additionally, empty selectors (originating from eliminated includes) indicate to fetch all scalar properties too.

            selectedColumns = tableAccessor.Source.Columns.Where(column => column.Type == ColumnType.Scalar).ToHashSet();
        }

        foreach ((ResourceFieldAttribute field, QueryLayer? nextLayer) in selectors.OrderBy(selector => selector.Key.PublicName))
        {
            if (field is AttrAttribute attribute)
            {
                // Returns null when the set contains an unmapped column, which is silently ignored.
                ColumnNode? column = tableAccessor.Source.FindColumn(attribute.Property.Name, ColumnType.Scalar, tableAccessor.Source.Alias);

                if (column != null)
                {
                    selectedColumns.Add(column);
                }
            }

            if (field is RelationshipAttribute relationship && nextLayer != null)
            {
                nextLayers.Add(relationship, nextLayer);
            }
        }

        if (_selectShape == SelectShape.Columns)
        {
            // Must store the selected columns *before* processing related tables, which may result in push down.
            SetColumnSelectors(tableAccessor, selectedColumns);
        }

        foreach ((RelationshipAttribute relationship, QueryLayer nextLayer) in nextLayers)
        {
            TableAccessorNode relatedTableAccessor = GetOrCreateRelatedTable(tableAccessor, relationship);
            ConvertQueryLayer(nextLayer, relatedTableAccessor);
        }
    }

    private void SetColumnSelectors(TableAccessorNode tableAccessor, IEnumerable<ColumnNode> columns)
    {
        if (!_selectorsPerTable.ContainsKey(tableAccessor))
        {
            throw new InvalidOperationException($"Table {tableAccessor.Source.Alias} not found in selected tables.");
        }

        // When selecting from a table, use a deterministic order to simplify test assertions.
        // When selecting from a sub-query (typically spanning multiple tables and renamed columns), existing order must be preserved.
        _selectorsPerTable[tableAccessor] = tableAccessor.Source is SelectNode
            ? PreserveColumnOrderEnsuringUniqueNames(columns)
            : OrderColumnsWithIdAtFrontEnsuringUniqueNames(columns);
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

    private TableAccessorNode GetOrCreateRelatedTable(TableAccessorNode leftTableAccessor, RelationshipAttribute relationship)
    {
        TableAccessorNode? relatedTableAccessor = _selectorsPerTable.Count == 0
            // Joining against something in an outer query.
            ? CreatePrimaryTableWithIdentityCondition(leftTableAccessor, relationship)
            : FindRelatedTable(leftTableAccessor, relationship);

        if (relatedTableAccessor == null)
        {
            if (relationship is HasManyAttribute && _limitOffset != null)
            {
                // Joining against a table that may produce multiple related rows results in incorrect pagination.
                // Therefore we need to push what we've built so far into a sub-query and join against that instead.
                PushDownIntoSubQuery();
            }

            relatedTableAccessor = CreateJoin(leftTableAccessor, relationship);
            TrackRelatedTable(leftTableAccessor, relationship, relatedTableAccessor);
        }

        return relatedTableAccessor;
    }

    private TableAccessorNode CreatePrimaryTableWithIdentityCondition(TableAccessorNode outerTableAccessor, RelationshipAttribute relationship)
    {
        TableAccessorNode innerTableAccessor = CreatePrimaryTable(relationship.RightType);

        ComparisonNode joinCondition = CreateJoinCondition(outerTableAccessor.Source, relationship, innerTableAccessor.Source);
        _whereConditions.Add(joinCondition);

        return innerTableAccessor;
    }

    private ComparisonNode CreateJoinCondition(TableSourceNode outerTableSource, RelationshipAttribute relationship, TableSourceNode innerTableSource)
    {
        RelationshipForeignKey foreignKey = _queryState.DataModelService.GetForeignKey(relationship);

        ColumnNode innerColumn = foreignKey.IsAtLeftSide
            ? innerTableSource.GetIdColumn(innerTableSource.Alias)
            : innerTableSource.GetColumn(foreignKey.ColumnName, ColumnType.ForeignKey, innerTableSource.Alias);

        ColumnNode outerColumn = foreignKey.IsAtLeftSide
            ? outerTableSource.GetColumn(foreignKey.ColumnName, ColumnType.ForeignKey, outerTableSource.Alias)
            : outerTableSource.GetIdColumn(outerTableSource.Alias);

        return new ComparisonNode(ComparisonOperator.Equals, outerColumn, innerColumn);
    }

    private TableAccessorNode? FindRelatedTable(TableAccessorNode leftTableAccessor, RelationshipAttribute relationship)
    {
        Dictionary<RelationshipAttribute, TableAccessorNode> rightTableAccessors = _queryState.RelatedTables[leftTableAccessor];
        return rightTableAccessors.TryGetValue(relationship, out TableAccessorNode? rightTableAccessor) ? rightTableAccessor : null;
    }

    private void PushDownIntoSubQuery()
    {
        string[] oldTableAliases = _selectorsPerTable.Keys.Select(tableAccessor => tableAccessor.Source.Alias).Cast<string>().ToArray();

        var subSelectBuilder = new SelectStatementBuilder(this);

        // In the sub-query, select all columns, to enable referencing them from other locations in the query.
        // This usually produces unused selectors in sub-queries, which are removed afterwards.
        var selectorsToKeep = new Dictionary<TableAccessorNode, IReadOnlyList<SelectorNode>>(subSelectBuilder._selectorsPerTable);
        subSelectBuilder.SelectAllColumnsInAllTables(selectorsToKeep.Keys);

        SelectNode subQuery = subSelectBuilder.ToSelect(true);

        // Push down potentially combines multiple tables with duplicate column names. Duplicate names are normally fine, but here
        // it must be possible to reference all columns from other locations in the query.
        AssertUniqueColumnNames(subQuery);

        ResetState(_selectShape, true);

        TableAccessorNode outerTableAccessor = CreatePrimaryTable(subQuery);
        var aliasedSubQuery = (SelectNode)outerTableAccessor.Source;

        // Store old-to-new table aliases, to enable rewriting stale column references afterwards.
        MapOldTableAliasesToSubQuery(aliasedSubQuery.Alias!, oldTableAliases);

        // In the outer query, select only what was originally set. Map references into outer query.
        _selectorsPerTable[outerTableAccessor] = MapSelectorsFromSubQuery(selectorsToKeep.SelectMany(selector => selector.Value), aliasedSubQuery);

        // Ordering must always be duplicated in the outer query, to achieve total ordering. Map references into outer query.
        _orderByTerms.AddRange(MapOrderByFromSubQuery(aliasedSubQuery));

        // Signals to rewrite stale columns before returning the final query.
        _queryState.HasPushDownOccurred = true;
    }

    private void SelectAllColumnsInAllTables(IEnumerable<TableAccessorNode> tableAccessors)
    {
        _selectorsPerTable.Clear();
        _selectorNamesUsed.Clear();

        foreach (TableAccessorNode tableAccessor in tableAccessors)
        {
            _selectorsPerTable.Add(tableAccessor, Array.Empty<SelectorNode>());

            if (_selectShape == SelectShape.Columns)
            {
                SetColumnSelectors(tableAccessor, tableAccessor.Source.Columns);
            }
        }
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

    private void MapOldTableAliasesToSubQuery(string newTableAlias, IEnumerable<string> oldTableAliases)
    {
        foreach (string oldTableAlias in oldTableAliases)
        {
            _queryState.OldToNewTableAliasMap[oldTableAlias] = newTableAlias;
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
                // If there's an alias, we should use it. Otherwise we could fallback to ordinal selector.
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

    private TableAccessorNode CreateJoin(TableAccessorNode leftTableAccessor, RelationshipAttribute relationship)
    {
        RelationshipForeignKey foreignKey = _queryState.DataModelService.GetForeignKey(relationship);

        IReadOnlyDictionary<string, ResourceFieldAttribute?> columnMappings = _queryState.DataModelService.GetColumnMappings(relationship.RightType);
        var rightTable = new TableNode(relationship.RightType, columnMappings, _queryState.TableAliasGenerator.GetNext());

        ComparisonNode joinCondition = CreateJoinCondition(leftTableAccessor.Source, relationship, rightTable);

        JoinType joinType = foreignKey.IsNullable && !_forceInnerJoins ? JoinType.LeftJoin : JoinType.InnerJoin;
        return new JoinNode(joinType, rightTable, (ColumnNode)joinCondition.Left, (ColumnNode)joinCondition.Right);
    }

    private void TrackRelatedTable(TableAccessorNode leftTableAccessor, RelationshipAttribute relationship, TableAccessorNode rightTableAccessor)
    {
        _queryState.RelatedTables.Add(rightTableAccessor, new Dictionary<RelationshipAttribute, TableAccessorNode>());
        _selectorsPerTable[rightTableAccessor] = Array.Empty<SelectorNode>();

        _queryState.RelatedTables[leftTableAccessor].Add(relationship, rightTableAccessor);
    }

    private SelectNode ToSelect(bool isSubQuery)
    {
        FilterNode? where = GetWhere();
        OrderByNode? orderBy = !_orderByTerms.Any() ? null : new OrderByNode(_orderByTerms);

        // Materialization using Dapper requires selectors to match property names, so adjust selector names accordingly.
        Dictionary<TableAccessorNode, IReadOnlyList<SelectorNode>> selectorsPerTable =
            isSubQuery ? _selectorsPerTable : AliasSelectorsToTableColumnNames(_selectorsPerTable);

        return new SelectNode(selectorsPerTable, where, orderBy, _limitOffset, null);
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

        // Collapse multiple ANDs at top-level. This turns "A AND (B AND C)" into "A AND B AND C".
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
            return join.InnerColumn;
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
                ColumnNode? column = currentAccessor.Source.FindColumn(attribute.Property.Name, ColumnType.Scalar, currentAccessor.Source.Alias);

                if (column == null)
                {
                    // Unmapped columns cannot be translated to SQL.
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
        return _queryState.ParameterGenerator.Create(expression.TypedValue);
    }

    public override SqlTreeNode VisitNullConstant(NullConstantExpression expression, TableAccessorNode tableAccessor)
    {
        return _queryState.ParameterGenerator.Create(null);
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
        subSelectBuilder._forceInnerJoins = true;

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

        SelectNode select = ToSelect(true);
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

        var column = (ColumnNode)Visit(expression.TargetAttribute!, tableAccessor);
        return new OrderByColumnNode(column, expression.IsAscending);
    }

    public override SqlTreeNode VisitSort(SortExpression expression, TableAccessorNode tableAccessor)
    {
        OrderByTermNode[] terms = VisitSequence<SortElementExpression, OrderByTermNode>(expression.Elements, tableAccessor).ToArray();
        return new OrderByNode(terms);
    }

    public override SqlTreeNode VisitPagination(PaginationExpression expression, TableAccessorNode tableAccessor)
    {
        ParameterNode limitParameter = _queryState.ParameterGenerator.Create(expression.PageSize!.Value);

        ParameterNode? offsetParameter = !expression.PageNumber.Equals(PageNumber.ValueOne)
            ? _queryState.ParameterGenerator.Create(expression.PageSize.Value * (expression.PageNumber.OneBasedValue - 1))
            : null;

        return new LimitOffsetNode(limitParameter, offsetParameter);
    }

    public override SqlTreeNode VisitCount(CountExpression expression, TableAccessorNode tableAccessor)
    {
        var subSelectBuilder = new SelectStatementBuilder(this);
        subSelectBuilder.ResetState(SelectShape.Count, true);
        subSelectBuilder._forceInnerJoins = true;

        return subSelectBuilder.GetCountClause(expression, tableAccessor);
    }

    private CountNode GetCountClause(CountExpression expression, TableAccessorNode outerTableAccessor)
    {
        _ = Visit(expression.TargetCollection, outerTableAccessor);

        SelectNode select = ToSelect(true);
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

    private sealed class QueryState
    {
        // Provides access to the underlying data model (tables, columns and foreign keys).
        public IDataModelService DataModelService { get; }

        // Used to generate unique aliases for tables.
        public TableAliasGenerator TableAliasGenerator { get; }

        // Used to generate unique parameters for constants (to improve query plan caching and guard against SQL injection).
        public ParameterGenerator ParameterGenerator { get; }

        public ILoggerFactory LoggerFactory { get; }

        // Prevents importing a table multiple times and enables to reference a table imported by an inner/outer query.
        // In case of push down, this may include temporary tables that won't survive in the final query.
        public Dictionary<TableAccessorNode, Dictionary<RelationshipAttribute, TableAccessorNode>> RelatedTables { get; } = new();

        // In case of push down, we track old/new table aliases, so we can rewrite stale references afterwards.
        // This cannot be done reliably during push down, because references to tables are on method call stacks.
        public Dictionary<string, string> OldToNewTableAliasMap { get; } = new();

        // Prevents emitting multiple levels of pagination, which requires multiple push downs with non-standard SQL constructs,
        // such as "ROW_NUMBER() OVER (PARTITION ...)" and LITERAL/OUTER APPLY JOINs.
        public bool HasPagination { get; set; }

        // Indicates whether push down into sub-query has occurred. If so, we need to rewrite stale references afterwards.
        public bool HasPushDownOccurred { get; set; }

        public QueryState(IDataModelService dataModelService, TableAliasGenerator tableAliasGenerator, ParameterGenerator parameterGenerator,
            ILoggerFactory loggerFactory)
        {
            ArgumentGuard.NotNull(dataModelService);
            ArgumentGuard.NotNull(tableAliasGenerator);
            ArgumentGuard.NotNull(parameterGenerator);
            ArgumentGuard.NotNull(loggerFactory);

            DataModelService = dataModelService;
            TableAliasGenerator = tableAliasGenerator;
            ParameterGenerator = parameterGenerator;
            LoggerFactory = loggerFactory;
        }

        public void Reset()
        {
            TableAliasGenerator.Reset();
            ParameterGenerator.Reset();

            RelatedTables.Clear();
            OldToNewTableAliasMap.Clear();
            HasPagination = false;
            HasPushDownOccurred = false;
        }
    }
}
