using System.Collections.Immutable;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Middleware;
using JsonApiDotNetCore.Queries;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Queries.QueryableBuilding;
using JsonApiDotNetCore.Resources.Annotations;

namespace JsonApiDotNetCore.Resources;
/// <summary>
/// Retrieves an <see cref="IResourceDefinition{TResource,TId}" /> instance from the D/I container and invokes a callback on it.
/// </summary>
public interface IResourceDefinitionAccessor<TIncludeElement, TFilter, TSort, TPagination, TSparseFields>
    where TIncludeElement : IQueryLayerIncludeElement
    where TFilter : IQueryLayerFilter
    where TSort : IQueryLayerSort
    where TPagination : IQueryLayerPagination
    where TSparseFields : IQueryLayerSparseFields
{
    /// <summary>
    /// Indicates whether this request targets only fetching of data (resources and relationships), as opposed to applying changes.
    /// </summary>
    /// <remarks>
    /// This property was added to reduce the impact of taking a breaking change. It will likely be removed in the next major version.
    /// </remarks>
    [Obsolete("Use IJsonApiRequest.IsReadOnly.")]
    bool IsReadOnlyRequest { get; }

    /// <summary>
    /// Gets an <see cref="IQueryableBuilder" /> instance from the service container.
    /// </summary>
    /// <remarks>
    /// This property was added to reduce the impact of taking a breaking change. It will likely be removed in the next major version.
    /// </remarks>
    [Obsolete("Use injected IQueryableBuilder instead.")]
    public IQueryableBuilder QueryableBuilder { get; }

    /// <summary>
    /// Invokes <see cref="IResourceDefinition{TResource,TId}.OnApplyIncludes" /> for the specified resource type.
    /// </summary>
    IImmutableSet<TIncludeElement> OnApplyIncludes(ResourceType resourceType, IImmutableSet<TIncludeElement> existingIncludes);

    /// <summary>
    /// Invokes <see cref="IResourceDefinition{TResource,TId}.OnApplyFilter" /> for the specified resource type.
    /// </summary>
    TFilter? OnApplyFilter(ResourceType resourceType, TFilter? existingFilter);

    /// <summary>
    /// Invokes <see cref="IResourceDefinition{TResource,TId}.OnApplySort" /> for the specified resource type.
    /// </summary>
    TSort? OnApplySort(ResourceType resourceType, TSort? existingSort);

    /// <summary>
    /// Invokes <see cref="IResourceDefinition{TResource,TId}.OnApplyPagination" /> for the specified resource type.
    /// </summary>
    TPagination? OnApplyPagination(ResourceType resourceType, TPagination? existingPagination);

    /// <summary>
    /// Invokes <see cref="IResourceDefinition{TResource,TId}.OnApplySparseFieldSet" /> for the specified resource type.
    /// </summary>
    TSparseFields? OnApplySparseFieldSet(ResourceType resourceType, TSparseFields? existingSparseFieldSet);

    /// <summary>
    /// Invokes <see cref="IResourceDefinition{TResource,TId}.OnRegisterQueryableHandlersForQueryStringParameters" /> for the specified resource type, then
    /// returns the <see cref="IQueryable{T}" /> expression for the specified parameter name.
    /// </summary>
    object? GetQueryableHandlerForQueryStringParameter(Type resourceClrType, string parameterName);

    /// <summary>
    /// Invokes <see cref="IResourceDefinition{TResource,TId}.GetMeta" /> for the specified resource.
    /// </summary>
#pragma warning disable AV1130 // Return type in method signature should be an interface to an unchangeable collection
    IDictionary<string, object?>? GetMeta(ResourceType resourceType, IIdentifiable resourceInstance);
#pragma warning restore AV1130 // Return type in method signature should be an interface to an unchangeable collection

    /// <summary>
    /// Invokes <see cref="IResourceDefinition{TResource,TId}.OnPrepareWriteAsync" /> for the specified resource.
    /// </summary>
    Task OnPrepareWriteAsync<TResource>(TResource resource, WriteOperationKind writeOperation, CancellationToken cancellationToken)
        where TResource : class, IIdentifiable;

    /// <summary>
    /// Invokes <see cref="IResourceDefinition{TResource,TId}.OnSetToOneRelationshipAsync" /> for the specified resource.
    /// </summary>
    public Task<IIdentifiable?> OnSetToOneRelationshipAsync<TResource>(TResource leftResource, HasOneAttribute hasOneRelationship,
        IIdentifiable? rightResourceId, WriteOperationKind writeOperation, CancellationToken cancellationToken)
        where TResource : class, IIdentifiable;

    /// <summary>
    /// Invokes <see cref="IResourceDefinition{TResource,TId}.OnSetToManyRelationshipAsync" /> for the specified resource.
    /// </summary>
    public Task OnSetToManyRelationshipAsync<TResource>(TResource leftResource, HasManyAttribute hasManyRelationship, ISet<IIdentifiable> rightResourceIds,
        WriteOperationKind writeOperation, CancellationToken cancellationToken)
        where TResource : class, IIdentifiable;

    /// <summary>
    /// Invokes <see cref="IResourceDefinition{TResource,TId}.OnAddToRelationshipAsync" /> for the specified resource.
    /// </summary>
    public Task OnAddToRelationshipAsync<TResource>(TResource leftResource, HasManyAttribute hasManyRelationship, ISet<IIdentifiable> rightResourceIds,
        CancellationToken cancellationToken)
        where TResource : class, IIdentifiable;

    /// <summary>
    /// Invokes <see cref="IResourceDefinition{TResource,TId}.OnRemoveFromRelationshipAsync" /> for the specified resource.
    /// </summary>
    public Task OnRemoveFromRelationshipAsync<TResource>(TResource leftResource, HasManyAttribute hasManyRelationship, ISet<IIdentifiable> rightResourceIds,
        CancellationToken cancellationToken)
        where TResource : class, IIdentifiable;

    /// <summary>
    /// Invokes <see cref="IResourceDefinition{TResource,TId}.OnWritingAsync" /> for the specified resource.
    /// </summary>
    Task OnWritingAsync<TResource>(TResource resource, WriteOperationKind writeOperation, CancellationToken cancellationToken)
        where TResource : class, IIdentifiable;

    /// <summary>
    /// Invokes <see cref="IResourceDefinition{TResource,TId}.OnWriteSucceededAsync" /> for the specified resource.
    /// </summary>
    Task OnWriteSucceededAsync<TResource>(TResource resource, WriteOperationKind writeOperation, CancellationToken cancellationToken)
        where TResource : class, IIdentifiable;

    /// <summary>
    /// Invokes <see cref="IResourceDefinition{TResource,TId}.OnDeserialize" /> for the specified resource.
    /// </summary>
    void OnDeserialize(IIdentifiable resource);

    /// <summary>
    /// Invokes <see cref="IResourceDefinition{TResource,TId}.OnSerialize" /> for the specified resource.
    /// </summary>
    void OnSerialize(IIdentifiable resource);
}


public interface IResourceDefinitionAccessor : IResourceDefinitionAccessor<IncludeElementExpression, FilterExpression, SortExpression, PaginationExpression, SparseFieldSetExpression>
{

}
