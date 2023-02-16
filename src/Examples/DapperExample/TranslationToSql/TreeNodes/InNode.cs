using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class InNode : FilterNode
{
    public TableColumnNode Column { get; }
    public IReadOnlyList<SqlValueNode> Values { get; }

    public InNode(TableColumnNode column, IReadOnlyList<SqlValueNode> values)
    {
        ArgumentGuard.NotNull(column);
        ArgumentGuard.NotNullNorEmpty(values);

        Column = column;
        Values = values;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitIn(this, argument);
    }
}
