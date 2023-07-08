using System.Linq.Expressions;
using System.Reflection;
using Antlr4.Runtime.Tree;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;


namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage.Visitors;
using DelegationFunc = Func<IJadncFilterVisitor<ExtendedQueryExpression>, IParseTree, ExtendedQueryExpression>;
public class ExprRuleVisitor : IJadncFilterRuleContextVisitor<JadncFiltersParser.ExprContext, ExtendedQueryExpression>
{
    private static Dictionary<Type, DelegationFunc> _delegates = new();

    private static MethodInfo _visitMethodInfo = typeof(IJadncFilterVisitor<ExtendedQueryExpression>).GetMethod("Visit")!;

    public IEnumerable<Type> HandlesRuleTypes => new[] { typeof(JadncFiltersParser.ExprContext) };

    private static DelegationFunc MakeDelegationFunc(Type ruleType)
    {
        var visitorParam = Expression.Parameter(typeof(IJadncFilterVisitor<ExtendedQueryExpression>));
        var contextParam = Expression.Parameter(typeof(IParseTree));
        var body = Expression.Call(visitorParam, _visitMethodInfo.MakeGenericMethod(ruleType), Expression.ConvertChecked(contextParam, ruleType));

        var lambda = Expression.Lambda<DelegationFunc>(body, visitorParam, contextParam);
        return lambda.Compile();
    }
    public ExtendedQueryExpression Visit(IJadncFilterVisitor<ExtendedQueryExpression> visitor, JadncFiltersParser.ExprContext context)
    {
        var child = context.GetChild(0);
        var concreteNodeType = child.GetType();
        if (!_delegates.ContainsKey(concreteNodeType))
        {
            _delegates[concreteNodeType] = MakeDelegationFunc(concreteNodeType);
        }
        return _delegates[concreteNodeType](visitor, child);
    }
    
}
