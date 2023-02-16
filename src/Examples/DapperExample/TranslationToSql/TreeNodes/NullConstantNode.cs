namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class NullConstantNode : SqlValueNode
{
    public static readonly NullConstantNode Instance = new();

    private NullConstantNode()
    {
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitNullConstant(this, argument);
    }
}
