using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace JsonApiDotNetCore.ExtendedQuery.QueryLanguage;

public interface IJadncFilterVisitor<TResult>
{
    TResult Visit<TParserRuleContext>(TParserRuleContext context) where TParserRuleContext : IParseTree;
}
