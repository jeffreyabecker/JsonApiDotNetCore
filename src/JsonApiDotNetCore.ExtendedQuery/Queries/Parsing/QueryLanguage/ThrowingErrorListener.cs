using Antlr4.Runtime;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage;
public class ThrowingErrorListener  : IAntlrErrorListener<int>, IAntlrErrorListener<IToken>
{    
    public  void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine,
        string msg, RecognitionException e)
    {
        throw new ExtendedQueryParseException(msg, offendingSymbol.StartIndex, e);
    }

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        throw new ExtendedQueryParseException(msg, charPositionInLine, e);
    }
}
