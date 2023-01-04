using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using JsonApiDotNetCore.OpenApi.JsonApiObjects.ResourceObjects;

namespace JsonApiDotNetCore.OpenApi.JsonApiObjects;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
internal abstract class NullableSingleData<TData>
    where TData : ResourceIdentifierObject
{
    [Required]
    [JsonPropertyName("data")]
    public TData? Data { get; set; }
}