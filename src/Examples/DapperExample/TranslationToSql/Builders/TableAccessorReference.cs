using DapperExample.TranslationToSql.TreeNodes;
using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.Builders;

internal sealed class TableAccessorReference : SqlValueNode
{
    public TableAccessorNode Value { get; set; }
    public string? TableAliasBeforePushDownIntoSubQuery { get; }

    public TableAccessorReference(TableAccessorNode value)
    {
        ArgumentGuard.NotNull(value);

        Value = value;
        TableAliasBeforePushDownIntoSubQuery = value.TableSource.Alias;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.Visit(Value, argument);
    }
}
