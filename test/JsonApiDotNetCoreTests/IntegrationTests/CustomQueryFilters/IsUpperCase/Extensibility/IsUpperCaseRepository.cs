using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Queries;
using JsonApiDotNetCore.Queries.Internal.QueryableBuilding;
using JsonApiDotNetCore.Repositories;
using JsonApiDotNetCore.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace JsonApiDotNetCoreTests.IntegrationTests.CustomQueryFilters.IsUpperCase.Extensibility;

internal sealed class IsUpperCaseRepository<TResource, TId> : EntityFrameworkCoreRepository<TResource, TId>
    where TResource : class, IIdentifiable<TId>
{
    private readonly IResourceFactory _resourceFactory;
    private readonly IModel _entityModel;

    public IsUpperCaseRepository(ITargetedFields targetedFields, IDbContextResolver dbContextResolver, IResourceGraph resourceGraph,
        IResourceFactory resourceFactory, IEnumerable<IQueryConstraintProvider> constraintProviders, ILoggerFactory loggerFactory,
        IResourceDefinitionAccessor resourceDefinitionAccessor)
        : base(targetedFields, dbContextResolver, resourceGraph, resourceFactory, constraintProviders, loggerFactory, resourceDefinitionAccessor)
    {
        DbContext dbContext = dbContextResolver.GetContext();

        _resourceFactory = resourceFactory;
        _entityModel = dbContext.Model;
    }

    protected override QueryableBuilder CreateQueryableBuilder(IQueryable<TResource> source)
    {
        IQueryableFactory queryableFactory = new IsUpperCaseQueryableFactory(_resourceFactory);
        var nameFactory = new LambdaParameterNameFactory();

        return queryableFactory.CreateQueryableBuilder(source.Expression, source.ElementType, typeof(Queryable), nameFactory, _entityModel, null);
    }
}
