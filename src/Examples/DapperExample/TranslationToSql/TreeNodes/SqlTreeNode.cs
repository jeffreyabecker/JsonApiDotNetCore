using DapperExample.TranslationToSql.Builders;

namespace DapperExample.TranslationToSql.TreeNodes;

internal abstract class SqlTreeNode
{
    public abstract TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument);

    public override string ToString()
    {
        var queryBuilder = new SqlQueryBuilder();
        return queryBuilder.GetCommand(this);
    }
}
