namespace DapperExample.TranslationToSql.TreeNodes;

/// <summary>
/// Represents a FROM clause. For example: FROM Customers AS t1
/// </summary>
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
