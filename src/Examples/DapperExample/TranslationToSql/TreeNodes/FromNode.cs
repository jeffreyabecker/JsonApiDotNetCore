namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class FromNode : TableAccessorNode
{
    public FromNode(TableSourceNode source)
        : base(source)
    {
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitFrom(this, argument);
    }
}
