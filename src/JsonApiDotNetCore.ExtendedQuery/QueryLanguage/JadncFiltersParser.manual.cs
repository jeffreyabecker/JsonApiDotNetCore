using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.EntityFrameworkCore.Metadata;

namespace JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
public partial class JadncFiltersParser
{
    public partial class ExprContext { public virtual TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) => visitor.Visit(this); }
    public partial class OfTypeExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class HasExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class InExprContext : ExprContext, IHaveSubExpr {
        public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); }
        public string Operator => K_NOT() != null ? "not in" : "in";
        public ExprContext Left => (ExprContext)expr(0);

    }
    public partial class NestedExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class OrExprContext : ExprContext, IBinaryExprNode { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class GreaterLessExprContext : ExprContext, IBinaryExprNode { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class FunctionExprContext : ExprContext, IHaveSubExpr { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class NotExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class AddExprContext : ExprContext, IBinaryExprNode { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class IsNullExprContext : ExprContext {
        public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); }
        public string Operator => K_NOT() != null ? "is not null" : "is null";
    }
    public partial class LiteralExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class MulExprContext : ExprContext, IBinaryExprNode { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class LikeExprContext : ExprContext, IBinaryExprNode {
        public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); }
        public string Operator => K_NOT() != null ? "not like" : "like";
    }
    public partial class IfExprContext : ExprContext, IHaveSubExpr {
        public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); }
        public ExprContext Condition => expr(0);
        public ExprContext WhenTrue => expr(1);
        public ExprContext WhenFalse => expr(2);
    }
    public partial class EqualExprContext : ExprContext, IBinaryExprNode { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class AndExprContext : ExprContext, IBinaryExprNode { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class IdentifierExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }

    public interface IHaveSubExpr : IParseTree
    {
        T GetRuleContext<T>(int i) where T : ParserRuleContext;
        T[] GetRuleContexts<T>() where T : ParserRuleContext;

#pragma warning disable IDE1006 // Naming Styles -- function name is from generated code
        public ExprContext[] expr()
        {
            return GetRuleContexts<ExprContext>();
        }
        public ExprContext expr(int i)
        {
            return GetRuleContext<ExprContext>(i);
        }
#pragma warning restore IDE1006 // Naming Styles
    }
    public interface IBinaryExprNode : IHaveSubExpr
    {
        ExprContext Left => (ExprContext)expr(0);
        ExprContext Right => (ExprContext)expr(1);
        string Operator => ((ITerminalNode)GetChild(1)).GetText();
    }

}
