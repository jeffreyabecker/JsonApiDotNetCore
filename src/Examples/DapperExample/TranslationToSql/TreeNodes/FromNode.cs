namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class FromNode : TableSourceNode
{
    public FromNode(TableNode table)
        : base(table)
    {
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitFrom(this, argument);
    }
}
