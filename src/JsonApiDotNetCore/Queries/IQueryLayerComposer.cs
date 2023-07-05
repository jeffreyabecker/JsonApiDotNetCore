using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;

namespace JsonApiDotNetCore.Queries;

/// <summary>
/// Takes scoped expressions from <see cref="IQueryConstraintProvider" />s and transforms them.
/// </summary>
public interface IQueryLayerComposer<TQueryLayer, TFilterExpression>
{
    /// <summary>
    /// Builds a filter from constraints, used to determine total resource count on a primary collection endpoint.
    /// </summary>
    TFilterExpression? GetPrimaryFilterFromConstraints(ResourceType primaryResourceType);

    /// <summary>
    /// Builds a filter from constraints, used to determine total resource count on a secondary collection endpoint.
    /// </summary>
    TFilterExpression? GetSecondaryFilterFromConstraints<TId>(TId primaryId, HasManyAttribute hasManyRelationship);

    /// <summary>
    /// Collects constraints and builds a <see cref="TQueryLayer" /> out of them, used to retrieve the actual resources.
    /// </summary>
    TQueryLayer ComposeFromConstraints(ResourceType requestResourceType);

    /// <summary>
    /// Collects constraints and builds a <see cref="TQueryLayer" /> out of them, used to retrieve one resource.
    /// </summary>
    TQueryLayer ComposeForGetById<TId>(TId id, ResourceType primaryResourceType, TopFieldSelection fieldSelection);

    /// <summary>
    /// Collects constraints and builds the secondary layer for a relationship endpoint.
    /// </summary>
    TQueryLayer ComposeSecondaryLayerForRelationship(ResourceType secondaryResourceType);

    /// <summary>
    /// Wraps a layer for a secondary endpoint into a primary layer, rewriting top-level includes.
    /// </summary>
    TQueryLayer WrapLayerForSecondaryEndpoint<TId>(TQueryLayer secondaryLayer, ResourceType primaryResourceType, TId primaryId,
        RelationshipAttribute relationship);

    /// <summary>
    /// Builds a query that retrieves the primary resource, including all of its attributes and all targeted relationships, during a create/update/delete
    /// request.
    /// </summary>
    TQueryLayer ComposeForUpdate<TId>(TId id, ResourceType primaryResourceType);

    /// <summary>
    /// Builds a query for each targeted relationship with a filter to match on its right resource IDs.
    /// </summary>
    IEnumerable<(TQueryLayer, RelationshipAttribute)> ComposeForGetTargetedSecondaryResourceIds(IIdentifiable primaryResource);

    /// <summary>
    /// Builds a query for the specified relationship with a filter to match on its right resource IDs.
    /// </summary>
    TQueryLayer ComposeForGetRelationshipRightIds(RelationshipAttribute relationship, ICollection<IIdentifiable> rightResourceIds);

    /// <summary>
    /// Builds a query for a to-many relationship with a filter to match on its left and right resource IDs.
    /// </summary>
    TQueryLayer ComposeForHasMany<TId>(HasManyAttribute hasManyRelationship, TId leftId, ICollection<IIdentifiable> rightResourceIds);
}

public interface IQueryLayerComposer: IQueryLayerComposer<QueryLayer, FilterExpression>
{

}
