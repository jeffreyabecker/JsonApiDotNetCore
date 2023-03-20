using DapperExample.TranslationToSql.TreeNodes;
using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.Builders;

// TODO: Remove this type.
internal sealed class TableAccessorReference : SqlValueNode
{
    public TableAccessorNode Value { get; }

    public TableAccessorReference(TableAccessorNode value)
    {
        ArgumentGuard.NotNull(value);

        Value = value;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.Visit(Value, argument);
    }
}
