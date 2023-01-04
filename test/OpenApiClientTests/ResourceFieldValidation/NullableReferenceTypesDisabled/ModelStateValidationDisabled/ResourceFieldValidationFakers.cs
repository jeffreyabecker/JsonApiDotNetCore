using Bogus;
using OpenApiClientTests.ResourceFieldValidation.NullableReferenceTypesDisabled.ModelStateValidationDisabled.GeneratedCode;

namespace OpenApiClientTests.ResourceFieldValidation.NullableReferenceTypesDisabled.ModelStateValidationDisabled;

internal sealed class ResourceFieldValidationFakers
{
    private readonly Lazy<Faker<ResourceAttributesInPostRequest>> _lazyPostAttributesFaker = new(() =>
        FakerFactory.Instance.Create<ResourceAttributesInPostRequest>());

    private readonly Lazy<Faker<ResourceAttributesInPatchRequest>> _lazyPatchAttributesFaker = new(() =>
        FakerFactory.Instance.Create<ResourceAttributesInPatchRequest>());

    private readonly Lazy<Faker<NullableToOneEmptyResourceInRequest>> _lazyNullableToOneFaker = new(() =>
        FakerFactory.Instance.CreateForObjectWithResourceId<NullableToOneEmptyResourceInRequest, int>());

    private readonly Lazy<Faker<ToManyEmptyResourceInRequest>> _lazyToManyFaker = new(() =>
        FakerFactory.Instance.CreateForObjectWithResourceId<ToManyEmptyResourceInRequest, int>());

    public Faker<ResourceAttributesInPostRequest> PostAttributes => _lazyPostAttributesFaker.Value;
    public Faker<ResourceAttributesInPatchRequest> PatchAttributes => _lazyPatchAttributesFaker.Value;
    public Faker<NullableToOneEmptyResourceInRequest> NullableToOne => _lazyNullableToOneFaker.Value;
    public Faker<ToManyEmptyResourceInRequest> ToMany => _lazyToManyFaker.Value;
}