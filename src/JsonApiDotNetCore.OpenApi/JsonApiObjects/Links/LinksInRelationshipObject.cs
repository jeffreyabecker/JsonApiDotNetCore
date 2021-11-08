using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

#pragma warning disable 8618 // Non-nullable member is uninitialized.

namespace JsonApiDotNetCore.OpenApi.JsonApiObjects.Links
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    internal sealed class LinksInRelationshipObject
    {
        [Required]
        public string Self { get; set; }

        [Required]
        public string Related { get; set; }
    }
}
