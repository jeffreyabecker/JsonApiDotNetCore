using JsonApiDotNetCore.QueryStrings.FieldChains;
using Microsoft.Extensions.Logging;
using JsonApiDotNetCore.Configuration;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage.FieldChains;

public class IdentiferChainMatcher
{
    private IdentifierChainPattern _identifierChainPattern;
    private FieldChainPatternMatchOptions _options;
    private ILogger<IdentiferChainMatcher> _logger;

    public IdentiferChainMatcher(IdentifierChainPattern identifierChainPattern, FieldChainPatternMatchOptions options, ILogger<IdentiferChainMatcher> logger)
    {
        _identifierChainPattern = identifierChainPattern;
        _options = options;
        _logger = logger;
    }

    public IdentifierChainMatchResult Match(string[] identifierChain, ResourceType resourceType)
    {
        throw new NotImplementedException();
    }
}
