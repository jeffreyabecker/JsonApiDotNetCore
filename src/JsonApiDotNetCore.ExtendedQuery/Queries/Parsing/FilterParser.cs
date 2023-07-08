using Antlr4.Runtime;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.ExtendedQuery.Queries.Expressions;
using JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
using JsonApiDotNetCore.ExtendedQuery.QueryLanguage;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Queries.Parsing;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing;
public class FilterParser : IFilterParser
{
    public FilterExpression Parse(string source, ResourceType resourceType)
    {
        ArgumentGuard.NotNull(resourceType);
        if (string.IsNullOrEmpty(source))
        {
            return new WrapperExpression(new EmptyExpression());
        }
        return new WrapperExpression(ParseExtendedFilter(source, resourceType));
    }

    public static ExtendedQueryExpression ParseExtendedFilter(string source, ResourceType resourceType)
    {
        ArgumentGuard.NotNullNorEmpty(source);
        var throwingErrorListener = new ThrowingErrorListener();
        var inputStream = new Antlr4.Runtime.AntlrInputStream(source);
        var lexer = new JadncFiltersLexer(inputStream);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(throwingErrorListener);
        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new JadncFiltersParser(commonTokenStream);
        parser.RemoveErrorListeners();        
        parser.AddErrorListener(throwingErrorListener);
        var expression = parser.expr();
        if(expression == null)
        {
            throw new ExtendedQueryParseException("unable to find filter expression", 0);
        }
        var treebuilder = new JadncFiltersToExtendedQueryConverter(resourceType);
        var result = treebuilder.Visit(expression);
        return result;
    }

}
