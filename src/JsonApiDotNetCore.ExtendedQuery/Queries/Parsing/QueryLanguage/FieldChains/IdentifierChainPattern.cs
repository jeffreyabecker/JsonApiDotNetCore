using JsonApiDotNetCore.QueryStrings.FieldChains;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using JsonApiDotNetCore.Configuration;

namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage.FieldChains;
public class IdentifierChainPattern
{
    public IdentifierChainPattern(IdentifierChainPatternElement[] elements)
    {

    }

    /// <summary>
    /// Matches the specified resource field chain against this pattern.
    /// </summary>
    /// <param name="identifierChain">
    /// The dot-separated chain of resource field names.
    /// </param>
    /// <param name="resourceType">
    /// The parent resource type to start matching from.
    /// </param>
    /// <param name="options">
    /// Match options, defaults to <see cref="FieldChainPatternMatchOptions.None" />.
    /// </param>
    /// <param name="loggerFactory">
    /// When provided, logs the matching steps at <see cref="LogLevel.Trace" /> level.
    /// </param>
    /// <returns>
    /// The match result.
    /// </returns>
    public IdentifierChainMatchResult Match(string[] identifierChain, ResourceType resourceType, FieldChainPatternMatchOptions options = FieldChainPatternMatchOptions.None,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentGuard.NotNullNorEmpty(identifierChain);
        ArgumentGuard.NotNull(resourceType);

        ILogger<IdentiferChainMatcher> logger = loggerFactory == null ? NullLogger<IdentiferChainMatcher>.Instance : loggerFactory.CreateLogger<IdentiferChainMatcher>();
        var matcher = new IdentiferChainMatcher(this, options, logger);
        return matcher.Match(identifierChain, resourceType);
    }

}
