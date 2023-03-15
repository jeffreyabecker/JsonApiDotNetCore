using System.Text;
using DapperExample.TranslationToSql.TreeNodes;
using JsonApiDotNetCore.Queries.Expressions;

namespace DapperExample.TranslationToSql.Builders;

internal sealed class SqlQueryBuilder : SqlTreeNodeVisitor<StringBuilder, object?>
{
    private readonly Dictionary<string, ParameterNode> _parametersByName = new();
    private int _indentDepth;

    public IDictionary<string, object?> Parameters => _parametersByName.Values.ToDictionary(parameter => parameter.Name, parameter => parameter.Value);

    public string GetCommand(SqlTreeNode node)
    {
        ResetState();

        var builder = new StringBuilder();
        Visit(node, builder);
        return builder.ToString();
    }

    private void ResetState()
    {
        _parametersByName.Clear();
        _indentDepth = 0;
    }

    public override object? VisitSelect(SelectNode node, StringBuilder builder)
    {
        if (builder.Length > 0)
        {
            using (Indent())
            {
                builder.Append('(');
                InnerVisitSelect(node, builder);
            }

            AppendOnNewLine(")", builder);
        }
        else
        {
            InnerVisitSelect(node, builder);
        }

        VisitAlias(node.Alias, builder);
        return null;
    }

    private void InnerVisitSelect(SelectNode node, StringBuilder builder)
    {
        AppendOnNewLine("SELECT ", builder);

        IEnumerable<SelectorNode> selectors = node.Selectors.SelectMany(selector => selector.Value);
        VisitSequence(selectors, builder);

        foreach (TableAccessorNode tableAccessor in node.Selectors.Keys)
        {
            Visit(tableAccessor, builder);
        }

        if (node.Where != null)
        {
            AppendOnNewLine("WHERE ", builder);
            Visit(node.Where, builder);
        }

        if (node.OrderBy != null)
        {
            Visit(node.OrderBy, builder);
        }

        if (node.LimitOffset != null)
        {
            Visit(node.LimitOffset, builder);
        }
    }

    public override object? VisitInsert(InsertNode node, StringBuilder builder)
    {
        AppendOnNewLine("INSERT INTO ", builder);
        Visit(node.Table, builder);
        builder.Append(" (");
        VisitSequence(node.Assignments.Select(assignment => assignment.Column), builder);
        builder.Append(')');

        AppendOnNewLine("VALUES (", builder);
        VisitSequence(node.Assignments.Select(assignment => assignment.Value), builder);
        builder.Append(')');

        ColumnNode idColumn = node.Table.GetIdColumn();
        AppendOnNewLine("RETURNING ", builder);
        Visit(idColumn, builder);
        return null;
    }

    public override object? VisitUpdate(UpdateNode node, StringBuilder builder)
    {
        AppendOnNewLine("UPDATE ", builder);
        Visit(node.Table, builder);

        AppendOnNewLine("SET ", builder);
        VisitSequence(node.Assignments, builder);

        AppendOnNewLine("WHERE ", builder);
        Visit(node.Where, builder);
        return null;
    }

    public override object? VisitDelete(DeleteNode node, StringBuilder builder)
    {
        AppendOnNewLine("DELETE FROM ", builder);
        Visit(node.Table, builder);

        AppendOnNewLine("WHERE ", builder);
        Visit(node.Where, builder);
        return null;
    }

    public override object? VisitTable(TableNode node, StringBuilder builder)
    {
        string tableName = FormatIdentifier(node.Name);
        builder.Append(tableName);
        VisitAlias(node.Alias, builder);
        return null;
    }

    public override object? VisitFrom(FromNode node, StringBuilder builder)
    {
        AppendOnNewLine("FROM ", builder);
        Visit(node.TableSource, builder);
        return null;
    }

    public override object? VisitJoin(JoinNode node, StringBuilder builder)
    {
        string joinTypeText = node.JoinType switch
        {
            JoinType.InnerJoin => "INNER JOIN ",
            JoinType.LeftJoin => "LEFT JOIN ",
            _ => throw new NotSupportedException($"Unknown join type '{node.JoinType}'.")
        };

        AppendOnNewLine(joinTypeText, builder);
        Visit(node.TableSource, builder);
        builder.Append(" ON ");
        Visit(node.ParentJoinColumn, builder);
        builder.Append(" = ");
        Visit(node.JoinColumn, builder);
        return null;
    }

    public override object? VisitColumn(ColumnNode node, StringBuilder builder)
    {
        if (node.TableAlias != null)
        {
            builder.Append($"{node.TableAlias}.");
        }

        string columnName = FormatIdentifier(node.Name);
        builder.Append(columnName);
        return null;
    }

    public override object? VisitColumnSelector(ColumnSelectorNode node, StringBuilder builder)
    {
        Visit(node.Column, builder);
        VisitAlias(node.Alias, builder);
        return null;
    }

    public override object? VisitOneSelector(OneSelectorNode node, StringBuilder builder)
    {
        builder.Append('1');
        VisitAlias(node.Alias, builder);
        return null;
    }

    public override object? VisitCountSelector(CountSelectorNode node, StringBuilder builder)
    {
        builder.Append("COUNT(*)");
        VisitAlias(node.Alias, builder);
        return null;
    }

    public override object? VisitRowNumber(RowNumberNode node, StringBuilder builder)
    {
        builder.Append("ROW_NUMBER() OVER (");

        using (Indent())
        {
            if (node.PartitionBy != null)
            {
                AppendOnNewLine("PARTITION BY ", builder);
                Visit(node.PartitionBy, builder);
            }

            VisitOrderBy(node.OrderBy, builder);
        }

        AppendOnNewLine(")", builder);
        VisitAlias(node.Alias, builder);
        return null;
    }

    public override object? VisitNot(NotNode node, StringBuilder builder)
    {
        builder.Append("NOT (");
        Visit(node.Child, builder);
        builder.Append(')');
        return null;
    }

    public override object? VisitLogical(LogicalNode node, StringBuilder builder)
    {
        string operatorText = node.Operator switch
        {
            LogicalOperator.And => "AND",
            LogicalOperator.Or => "OR",
            _ => throw new NotSupportedException($"Unknown logical operator '{node.Operator}'.")
        };

        builder.Append('(');
        Visit(node.Terms[0], builder);
        builder.Append(')');

        foreach (FilterNode nextTerm in node.Terms.Skip(1))
        {
            builder.Append($" {operatorText} (");
            Visit(nextTerm, builder);
            builder.Append(')');
        }

        return null;
    }

    public override object? VisitComparison(ComparisonNode node, StringBuilder builder)
    {
        string operatorText = node.Operator switch
        {
            ComparisonOperator.Equals => node.Left is NullConstantNode || node.Right is NullConstantNode ? "IS" : "=",
            ComparisonOperator.GreaterThan => ">",
            ComparisonOperator.GreaterOrEqual => ">=",
            ComparisonOperator.LessThan => "<",
            ComparisonOperator.LessOrEqual => "<=",
            _ => throw new NotSupportedException($"Unknown comparison operator '{node.Operator}'.")
        };

        Visit(node.Left, builder);
        builder.Append($" {operatorText} ");
        Visit(node.Right, builder);
        return null;
    }

    public override object? VisitLike(LikeNode node, StringBuilder builder)
    {
        Visit(node.Column, builder);
        builder.Append(" LIKE '");

        if (node.MatchKind is TextMatchKind.Contains or TextMatchKind.EndsWith)
        {
            builder.Append('%');
        }

        string escapedValue = node.Text.Replace("%", "\\%").Replace("_", "\\_");
        builder.Append(escapedValue);

        if (node.MatchKind is TextMatchKind.Contains or TextMatchKind.StartsWith)
        {
            builder.Append('%');
        }

        builder.Append('\'');
        return null;
    }

    public override object? VisitIn(InNode node, StringBuilder builder)
    {
        Visit(node.Column, builder);
        builder.Append(" IN (");
        VisitSequence(node.Values, builder);
        builder.Append(')');
        return null;
    }

    public override object? VisitExists(ExistsNode node, StringBuilder builder)
    {
        builder.Append("EXISTS ");
        Visit(node.SubSelect, builder);
        return null;
    }

    public override object? VisitCount(CountNode node, StringBuilder builder)
    {
        Visit(node.SubSelect, builder);
        return null;
    }

    public override object? VisitOrderBy(OrderByNode node, StringBuilder builder)
    {
        AppendOnNewLine("ORDER BY ", builder);
        VisitSequence(node.Terms, builder);
        return null;
    }

    public override object? VisitOrderByColumn(OrderByColumnNode node, StringBuilder builder)
    {
        Visit(node.Column, builder);

        if (!node.IsAscending)
        {
            builder.Append(" DESC");
        }

        return null;
    }

    public override object? VisitOrderByCount(OrderByCountNode node, StringBuilder builder)
    {
        Visit(node.Count, builder);

        if (!node.IsAscending)
        {
            builder.Append(" DESC");
        }

        return null;
    }

    public override object? VisitLimitOffset(LimitOffsetNode node, StringBuilder builder)
    {
        AppendOnNewLine("LIMIT ", builder);
        Visit(node.Limit, builder);

        if (node.Offset != null)
        {
            builder.Append(" OFFSET ");
            Visit(node.Offset, builder);
        }

        return null;
    }

    public override object? VisitColumnAssignment(ColumnAssignmentNode node, StringBuilder builder)
    {
        Visit(node.Column, builder);
        builder.Append(" = ");
        Visit(node.Value, builder);
        return null;
    }

    public override object? VisitParameter(ParameterNode node, StringBuilder builder)
    {
        _parametersByName[node.Name] = node;

        builder.Append(node.Name);
        return null;
    }

    public override object? VisitNullConstant(NullConstantNode node, StringBuilder builder)
    {
        builder.Append("NULL");
        return null;
    }

    private static void VisitAlias(string? alias, StringBuilder builder)
    {
        if (alias != null)
        {
            builder.Append($" AS {alias}");
        }
    }

    private void VisitSequence<T>(IEnumerable<T> elements, StringBuilder builder)
        where T : SqlTreeNode
    {
        bool isFirstElement = true;

        foreach (T element in elements)
        {
            if (isFirstElement)
            {
                isFirstElement = false;
            }
            else
            {
                builder.Append(", ");
            }

            Visit(element, builder);
        }
    }

    private void AppendOnNewLine(string? value, StringBuilder builder)
    {
        if (!string.IsNullOrEmpty(value))
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(new string(' ', _indentDepth * 4));
            builder.Append(value);
        }
    }

    internal static string FormatIdentifier(string value)
    {
        string escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private IDisposable Indent()
    {
        _indentDepth++;
        return new RevertIndentOnDispose(this);
    }

    private sealed class RevertIndentOnDispose : IDisposable
    {
        private readonly SqlQueryBuilder _owner;

        public RevertIndentOnDispose(SqlQueryBuilder owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            _owner._indentDepth--;
        }
    }
}
