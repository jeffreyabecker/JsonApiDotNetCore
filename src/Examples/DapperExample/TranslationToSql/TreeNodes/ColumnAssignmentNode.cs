using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class ColumnAssignmentNode : SqlTreeNode
{
    public TableColumnNode Column { get; }
    public SqlValueNode Value { get; }

    public ColumnAssignmentNode(TableColumnNode column, SqlValueNode value)
    {
        ArgumentGuard.NotNull(column);
        ArgumentGuard.NotNull(value);

        Column = column;
        Value = value;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitColumnAssignment(this, argument);
    }
}
