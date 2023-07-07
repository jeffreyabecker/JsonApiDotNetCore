using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
public abstract class ParseTreeVisitorBase<TResult> : IJadncFilterVisitor<TResult>
{
    private readonly Dictionary<Type, object> _typedVisitors = new Dictionary<Type, object>();
    protected ParseTreeVisitorBase(params object[] typedVisitors)
    {
        foreach(var typedVisitor in typedVisitors)
        {
            _typedVisitors[typedVisitor.GetType()] = typedVisitor;
        }
    }
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

    protected virtual TResult VisitDefault(IParseTree context)
    {
        throw new NotImplementedException($"Dont have visitor for {context.GetType().FullName}");
    }
}
