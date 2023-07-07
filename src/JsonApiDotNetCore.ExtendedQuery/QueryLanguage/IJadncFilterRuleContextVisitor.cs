using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
public interface IJadncFilterRuleContextVisitor<TParserRuleContext, TResult> where TParserRuleContext : IParseTree
{
    TResult Visit(IJadncFilterVisitor<TResult> visitor, TParserRuleContext ruleContext);
}
