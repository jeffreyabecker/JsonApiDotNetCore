using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
public abstract class ParseTreeVisitorBase<TResult> : IJadncFilterVisitor<TResult>
{
    private readonly Dictionary<Type, object> _typedVisitors = new Dictionary<Type, object>();

    public TResult Visit<TParserRuleContext>(TParserRuleContext context) where TParserRuleContext : IParseTree
    {
        if (_typedVisitors.ContainsKey(typeof(TParserRuleContext)))
        {
            return ((IJadncFilterRuleContextVisitor<TParserRuleContext, TResult>)_typedVisitors[typeof(TParserRuleContext)]).Visit(this, context);
        }
        return VisitDefault(context);
    }

    protected void Add<TParserRuleContext>(IJadncFilterRuleContextVisitor<TParserRuleContext, TResult> visitorStrategy) where TParserRuleContext : ParserRuleContext
    {
        _typedVisitors[typeof(TParserRuleContext)] = visitorStrategy;
    }

    protected virtual TResult VisitDefault(ParserRuleContext context)
    {
        throw new NotImplementedException($"Dont have visitor for {context.GetType().FullName}");
    }
}
