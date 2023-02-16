using JsonApiDotNetCore;
using JsonApiDotNetCore.Queries.Expressions;

namespace DapperExample.TranslationToSql.TreeNodes;

internal sealed class LikeNode : FilterNode
{
    public TableColumnNode Column { get; }
    public TextMatchKind MatchKind { get; }
    public string Text { get; }

    public LikeNode(TableColumnNode column, TextMatchKind matchKind, string text)
    {
        ArgumentGuard.NotNull(column);
        ArgumentGuard.NotNull(text);

        Column = column;
        MatchKind = matchKind;
        Text = text;
    }

    public override TResult Accept<TArgument, TResult>(SqlTreeNodeVisitor<TArgument, TResult> visitor, TArgument argument)
    {
        return visitor.VisitLike(this, argument);
    }
}
