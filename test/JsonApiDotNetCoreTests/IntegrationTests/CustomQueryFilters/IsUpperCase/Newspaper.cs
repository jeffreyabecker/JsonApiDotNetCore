using JetBrains.Annotations;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;

namespace JsonApiDotNetCoreTests.IntegrationTests.CustomQueryFilters.IsUpperCase;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
[Resource(ControllerNamespace = "JsonApiDotNetCoreTests.IntegrationTests.CustomQueryFilters.IsUpperCase")]
public sealed class Newspaper : Identifiable<long>
{
    [Attr]
    public DateTime PublicationDate { get; set; }

    [Attr]
    public string FrontPageHeadline { get; set; } = null!;

    [HasMany]
    public IList<NewsArticle> Articles { get; set; } = new List<NewsArticle>();
}
