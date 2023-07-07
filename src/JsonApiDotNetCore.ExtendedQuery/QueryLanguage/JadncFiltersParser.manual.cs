using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Queries.Parsing;
using JsonApiDotNetCore.QueryStrings.FieldChains;
using JsonApiDotNetCore.Resources.Annotations;
using Microsoft.AspNetCore.Routing.Template;

namespace JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
public partial class JadncFiltersParser
{
    public partial class ExprContext
    {
        public virtual TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor)
        {
            throw new NotImplementedException();
        }

    }
    public partial class OfTypeExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class HasExpressionContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class InExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class NestedExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class OrExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class GreaterLessExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class FunctionExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class NotExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class AddExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class IsNullExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class LiteralExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class MulExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class LikeExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class IfExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class EqualExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }
    public partial class AndExprContext : ExprContext { public override TResult Accept<TResult>(IJadncFilterVisitor<TResult> visitor) { return visitor.Visit(this); } }


}
