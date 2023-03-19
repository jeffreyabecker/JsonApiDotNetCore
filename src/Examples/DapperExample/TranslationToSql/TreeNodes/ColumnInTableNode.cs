namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class ColumnInTableNode : ColumnNode
{
    public ColumnInTableNode(string name, string? tableAlias)
        : base(name, tableAlias)
    {
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitColumnInTable(this, argument);
    }
}
