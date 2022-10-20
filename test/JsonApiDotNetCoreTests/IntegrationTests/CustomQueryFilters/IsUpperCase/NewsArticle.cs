using JetBrains.Annotations;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;

namespace JsonApiDotNetCoreTests.IntegrationTests.CustomQueryFilters.IsUpperCase;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
[Resource(ControllerNamespace = "JsonApiDotNetCoreTests.IntegrationTests.CustomQueryFilters.IsUpperCase")]
public sealed class NewsArticle : Identifiable<long>
{
    [Attr]
    public string Headline { get; set; } = null!;

    [Attr]
    public string Content { get; set; } = null!;

    [HasOne]
    public Newspaper? PublishedIn { get; set; }
}
