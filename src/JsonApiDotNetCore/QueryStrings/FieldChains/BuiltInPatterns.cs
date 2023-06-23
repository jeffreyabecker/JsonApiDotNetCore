#pragma warning disable AV1008 // Class should not be static

namespace JsonApiDotNetCore.QueryStrings.FieldChains;

internal static class BuiltInPatterns
{
    public static readonly FieldChainPattern SingleField = FieldChainPattern.Parse("F");
    public static readonly FieldChainPattern ToOneChain = FieldChainPattern.Parse("O+");
    public static readonly FieldChainPattern ToOneChainEndingInAttribute = FieldChainPattern.Parse("O*A");
    public static readonly FieldChainPattern ToOneChainEndingInAttributeOrToOne = FieldChainPattern.Parse("O*[OA]");
    public static readonly FieldChainPattern ToOneChainEndingInToMany = FieldChainPattern.Parse("O*M");
    public static readonly FieldChainPattern RelationshipChainEndingInToMany = FieldChainPattern.Parse("R*M");
}
