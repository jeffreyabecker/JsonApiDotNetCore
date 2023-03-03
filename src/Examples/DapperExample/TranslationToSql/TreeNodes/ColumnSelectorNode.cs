using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class ColumnSelectorNode : SelectorNode
{
    public ColumnNode Column { get; }

    public ColumnSelectorNode(ColumnNode column)
    {
        ArgumentGuard.NotNull(column);

        Column = column;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitColumnSelector(this, argument);
    }
}
