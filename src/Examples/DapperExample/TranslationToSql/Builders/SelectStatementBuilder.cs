using System.Net;
using DapperExample.TranslationToSql.DataModel;
using DapperExample.TranslationToSql.TreeNodes;
using JsonApiDotNetCore;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Errors;
using JsonApiDotNetCore.Queries;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;
using JsonApiDotNetCore.Serialization.Objects;

namespace DapperExample.TranslationToSql.Builders;

internal sealed class SelectStatementBuilder : QueryExpressionVisitor<TableAccessorNode, SqlTreeNode>
{
    private readonly IDataModelService _dataModelService;
    private readonly AliasGenerator _aliasGenerator;
    private readonly ParameterGenerator _parameterGenerator;
    private readonly Dictionary<TableAccessorNode, Dictionary<RelationshipAttribute, TableAccessorNode>> _relatedTables = new();
    private readonly Dictionary<TableAccessorNode, IReadOnlyList<SelectorNode>> _selectorsPerTable = new();
    private readonly List<FilterNode> _whereConditions = new();
    private readonly List<OrderByTermNode> _orderByTerms = new();
    private SelectShape _selectShape;
    private LimitOffsetNode? _limitOffset;

    public SelectStatementBuilder(IDataModelService dataModelService)
        : this(dataModelService, new AliasGenerator(), new ParameterGenerator())
    {
        ArgumentGuard.NotNull(dataModelService);
    }

    private SelectStatementBuilder(IDataModelService dataModelService, AliasGenerator aliasGenerator, ParameterGenerator parameterGenerator)
    {
        _dataModelService = dataModelService;
        _aliasGenerator = aliasGenerator;
        _parameterGenerator = parameterGenerator;
    }

    public SelectNode Build(QueryLayer queryLayer, SelectShape selectShape)
    {
        ResetState();
        _selectShape = selectShape;

        FromNode from = CreateFrom(queryLayer.ResourceType);
        ConvertQueryLayer(queryLayer, from);

        FilterNode? where = GetWhere();
        OrderByNode? orderBy = !_orderByTerms.Any() ? null : new OrderByNode(_orderByTerms);

        return new SelectNode(_selectorsPerTable, where, orderBy, _limitOffset, null);
    }

    private void ResetState()
    {
        _relatedTables.Clear();
        _selectorsPerTable.Clear();
        _aliasGenerator.Reset();
        _parameterGenerator.Reset();
    }

    private FromNode CreateFrom(ResourceType resourceType)
    {
        IReadOnlyDictionary<string, ResourceFieldAttribute?> columnMappings = _dataModelService.GetColumnMappings(resourceType);
        var table = new TableNode(resourceType, columnMappings, _aliasGenerator.GetNext());
        var from = new FromNode(table);

        IncludePrimaryTable(from);
        return from;
    }

    private FromNode CreateFrom(TableSourceNode tableSource)
    {
        TableSourceNode clone = tableSource.Clone(_aliasGenerator.GetNext());
        var from = new FromNode(clone);

        IncludePrimaryTable(from);
        return from;
    }

    private void IncludePrimaryTable(TableAccessorNode tableAccessor)
    {
        _relatedTables[tableAccessor] = new Dictionary<RelationshipAttribute, TableAccessorNode>();

        _selectorsPerTable[tableAccessor] = _selectShape switch
        {
            SelectShape.Columns => OrderColumnsWithIdAtFront(tableAccessor.TableSource.ScalarColumns),
            SelectShape.Count => new CountSelectorNode(null).AsList(),
            _ => new OneSelectorNode(null).AsList()
        };
    }

    private static List<SelectorNode> OrderColumnsWithIdAtFront(IEnumerable<ColumnNode> columns)
    {
        ColumnNode? idColumn = null;
        List<SelectorNode> otherColumns = new();

        foreach (ColumnNode column in columns.OrderBy(column => column.Name))
        {
            if (column.Name == nameof(Identifiable<object>.Id))
            {
                idColumn = column;
            }
            else
            {
                var columnSelector = new ColumnSelectorNode(column, null);
                otherColumns.Add(columnSelector);
            }
        }

        if (idColumn != null)
        {
            var idSelector = new ColumnSelectorNode(idColumn, null);
            otherColumns.Insert(0, idSelector);
        }

        return otherColumns;
    }

    private void ConvertQueryLayer(QueryLayer queryLayer, TableAccessorNode tableAccessor)
    {
        if (queryLayer.Include != null)
        {
            _ = Visit(queryLayer.Include, tableAccessor);
        }

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

        if (queryLayer.Pagination is { PageSize: { } })
        {
            // TODO: Push down into sub-select for non-top-level.
            if (_limitOffset == null)
            {
                _limitOffset = (LimitOffsetNode)Visit(queryLayer.Pagination, tableAccessor);
            }
        }

        if (queryLayer.Selection != null)
        {
            foreach (ResourceType resourceType in queryLayer.Selection.GetResourceTypes())
            {
                FieldSelectors selectors = queryLayer.Selection.GetOrCreateSelectors(resourceType);
                ConvertSelectors(selectors, tableAccessor);
            }
        }
    }

    private void ConvertSelectors(FieldSelectors selectors, TableAccessorNode tableAccessor)
    {
        HashSet<ColumnNode> selectedColumns = new();

        if (selectors.ContainsReadOnlyAttribute || selectors.ContainsOnlyRelationships)
        {
            // If a read-only attribute is selected, its calculated value likely depends on another property, so fetch all scalar properties.
            // And only selecting relationships implicitly means to fetch all scalar properties as well.

            selectedColumns = tableAccessor.TableSource.ScalarColumns.ToHashSet();
        }

        foreach ((ResourceFieldAttribute? field, QueryLayer? nextLayer) in selectors.OrderBy(selector => selector.Key.PublicName))
        {
            if (field is AttrAttribute attribute)
            {
                ColumnNode? column = tableAccessor.TableSource.FindScalarColumn(attribute.Property.Name);

                if (column != null)
                {
                    selectedColumns.Add(column);
                }
            }

            if (field is RelationshipAttribute relationship && nextLayer != null)
            {
                TableAccessorNode relatedTableAccessor = GetOrCreateRelatedTable(tableAccessor, relationship);
                ConvertQueryLayer(nextLayer, relatedTableAccessor);
            }
        }

        if (_selectShape == SelectShape.Columns)
        {
            _selectorsPerTable[tableAccessor] = OrderColumnsWithIdAtFront(selectedColumns);
        }
    }

    private TableAccessorNode GetOrCreateRelatedTable(TableAccessorNode leftTableAccessor, RelationshipAttribute relationship)
    {
        TableAccessorNode? relatedTableAccessor = FindRelatedTable(leftTableAccessor, relationship);

        if (relatedTableAccessor == null)
        {
            relatedTableAccessor = CreateJoin(leftTableAccessor, relationship);
            IncludeRelatedTable(leftTableAccessor, relationship, relatedTableAccessor);
        }

        return relatedTableAccessor;
    }

    private TableAccessorNode? FindRelatedTable(TableAccessorNode leftTableAccessor, RelationshipAttribute relationship)
    {
        if (_relatedTables.TryGetValue(leftTableAccessor, out Dictionary<RelationshipAttribute, TableAccessorNode>? rightTableAccessors))
        {
            if (rightTableAccessors.TryGetValue(relationship, out TableAccessorNode? rightTable))
            {
                return rightTable;
            }
        }

        return null;
    }

    private void IncludeRelatedTable(TableAccessorNode leftTableAccessor, RelationshipAttribute relationship, TableAccessorNode rightTableAccessor)
    {
        _relatedTables.TryAdd(leftTableAccessor, new Dictionary<RelationshipAttribute, TableAccessorNode>());
        _relatedTables[leftTableAccessor].Add(relationship, rightTableAccessor);

        _relatedTables[rightTableAccessor] = new Dictionary<RelationshipAttribute, TableAccessorNode>();
        _selectorsPerTable[rightTableAccessor] = new List<SelectorNode>();
    }

    private JoinNode CreateJoin(TableAccessorNode leftTableAccessor, RelationshipAttribute relationship)
    {
        RelationshipForeignKey foreignKey = _dataModelService.GetForeignKey(relationship);

        IReadOnlyDictionary<string, ResourceFieldAttribute?> columnMappings = _dataModelService.GetColumnMappings(relationship.RightType);
        var table = new TableNode(relationship.RightType, columnMappings, _aliasGenerator.GetNext());

        ColumnNode joinColumn = foreignKey.IsAtLeftSide ? table.GetIdColumn() : table.GetForeignKeyColumn(foreignKey.ColumnName);

        ColumnNode parentJoinColumn = foreignKey.IsAtLeftSide
            ? leftTableAccessor.TableSource.GetForeignKeyColumn(foreignKey.ColumnName)
            : leftTableAccessor.TableSource.GetIdColumn();

        return new JoinNode(foreignKey.IsNullable ? JoinType.LeftJoin : JoinType.InnerJoin, table, joinColumn, parentJoinColumn);
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
        TableAccessorNode currentTableAccessor = tableAccessor;

        foreach (ResourceFieldAttribute field in expression.Fields)
        {
            if (field is RelationshipAttribute relationship)
            {
                currentTableAccessor = GetOrCreateRelatedTable(currentTableAccessor, relationship);
            }
            else if (field is AttrAttribute attribute)
            {
                ColumnNode? column = currentTableAccessor.TableSource.FindScalarColumn(attribute.Property.Name);

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

        return currentTableAccessor;
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
        var subSelectBuilder = new SelectStatementBuilder(_dataModelService, _aliasGenerator, _parameterGenerator)
        {
            _selectShape = SelectShape.One
        };

        return subSelectBuilder.GetExistsClause(expression, tableAccessor.TableSource);
    }

    private ExistsNode GetExistsClause(HasExpression expression, TableSourceNode outerTableSource)
    {
        FromNode from = CreateFrom(outerTableSource);
        var rightTableAccessor = (TableAccessorNode)Visit(expression.TargetCollection, from);

        if (expression.Filter != null)
        {
            var filter = (FilterNode)Visit(expression.Filter, rightTableAccessor);
            _whereConditions.Add(filter);
        }

        var joinCondition = new ComparisonNode(ComparisonOperator.Equals, outerTableSource.GetIdColumn(), from.TableSource.GetIdColumn());
        _whereConditions.Add(joinCondition);

        FilterNode? where = GetWhere();

        var subSelect = new SelectNode(_selectorsPerTable, where, null, null, null);
        return new ExistsNode(subSelect);
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
        OrderByTermNode[] columns = VisitSequence<SortElementExpression, OrderByTermNode>(expression.Elements, tableAccessor).ToArray();
        return new OrderByNode(columns);
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
        var subSelectBuilder = new SelectStatementBuilder(_dataModelService, _aliasGenerator, _parameterGenerator)
        {
            _selectShape = SelectShape.Count
        };

        return subSelectBuilder.GetCountClause(expression, tableAccessor.TableSource);
    }

    private CountNode GetCountClause(CountExpression expression, TableSourceNode outerTableSource)
    {
        FromNode from = CreateFrom(outerTableSource);
        _ = Visit(expression.TargetCollection, from);

        var joinCondition = new ComparisonNode(ComparisonOperator.Equals, outerTableSource.GetIdColumn(), from.TableSource.GetIdColumn());
        _whereConditions.Add(joinCondition);

        FilterNode? where = GetWhere();

        var subSelect = new SelectNode(_selectorsPerTable, where, null, null, null);
        return new CountNode(subSelect);
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

    public override SqlTreeNode VisitInclude(IncludeExpression expression, TableAccessorNode tableAccessor)
    {
        foreach (IncludeElementExpression element in expression.Elements.OrderBy(element => element.Relationship.PublicName))
        {
            _ = Visit(element, tableAccessor);
        }

        return null!;
    }

    public override SqlTreeNode VisitIncludeElement(IncludeElementExpression expression, TableAccessorNode tableAccessor)
    {
        TableAccessorNode relatedTableAccessor = GetOrCreateRelatedTable(tableAccessor, expression.Relationship);

        _selectorsPerTable[relatedTableAccessor] = _selectShape == SelectShape.Columns
            ? OrderColumnsWithIdAtFront(relatedTableAccessor.TableSource.ScalarColumns)
            : new List<SelectorNode>();

        _ = VisitSequence<IncludeElementExpression, TableAccessorNode>(expression.Children.OrderBy(child => child.Relationship.PublicName),
            relatedTableAccessor).ToArray();

        return relatedTableAccessor;
    }
}
