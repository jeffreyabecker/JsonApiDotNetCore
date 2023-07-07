namespace JsonApiDotNetCore.ExtendedQuery.Queries.Parsing.QueryLanguage.FieldChains;

public record IdentifierChainPatternElement(IdentifierElementTypes AllowedTypes, bool AtLeastOne, bool AtMostOne);
