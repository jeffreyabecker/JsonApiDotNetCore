using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using JsonApiDotNetCore.OpenApi.JsonApiObjects.Links;
using JsonApiDotNetCore.OpenApi.JsonApiObjects.ResourceObjects;
using JsonApiDotNetCore.Resources;

#pragma warning disable 8618 // Non-nullable member is uninitialized.

namespace JsonApiDotNetCore.OpenApi.JsonApiObjects.RelationshipData
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    internal sealed class ToManyRelationshipResponseData<TResource> : ManyData<ResourceIdentifierObject<TResource>>
        where TResource : IIdentifiable
    {
        [Required]
        public LinksInRelationshipObject Links { get; set; }

        public IDictionary<string, object> Meta { get; set; }
    }
}
