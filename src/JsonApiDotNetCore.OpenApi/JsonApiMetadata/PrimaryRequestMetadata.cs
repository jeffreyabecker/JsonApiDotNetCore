using System;

namespace JsonApiDotNetCore.OpenApi.JsonApiMetadata
{
    internal sealed class PrimaryRequestMetadata : IJsonApiRequestMetadata
    {
        public Type Type { get; }

        public PrimaryRequestMetadata(Type type)
        {
            Type = type;
        }
    }
}
