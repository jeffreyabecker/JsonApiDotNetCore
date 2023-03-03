using JsonApiDotNetCore;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class ColumnNode : SqlValueNode
{
    public string Name { get; }
    public string? TableAlias { get; }

    public ColumnNode(string name, string? tableAlias)
    {
        ArgumentGuard.NotNullNorEmpty(name);

        Name = name;
        TableAlias = tableAlias;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitColumn(this, argument);
    }
}
