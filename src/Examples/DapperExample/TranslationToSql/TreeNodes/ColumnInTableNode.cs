namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class ColumnInTableNode : ColumnNode
{
    public ColumnInTableNode(string name, ColumnType type, string? tableAlias)
        : base(name, type, tableAlias)
    {
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitColumnInTable(this, argument);
    }
}
