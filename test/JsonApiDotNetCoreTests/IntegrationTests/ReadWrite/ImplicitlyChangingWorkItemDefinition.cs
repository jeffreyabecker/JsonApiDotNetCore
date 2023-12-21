using JetBrains.Annotations;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Middleware;
using JsonApiDotNetCore.Resources;
using Microsoft.EntityFrameworkCore;

namespace JsonApiDotNetCoreTests.IntegrationTests.ReadWrite;

/// <summary>
/// Used to simulate side effects that occur in the database while saving, typically caused by database triggers.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public sealed class ImplicitlyChangingWorkItemDefinition(IResourceGraph resourceGraph, ReadWriteDbContext dbContext)
    : JsonApiResourceDefinition<WorkItem, int>(resourceGraph)
{
    internal const string Suffix = " (changed)";

    private readonly ReadWriteDbContext _dbContext = dbContext;

    public override async Task OnWriteSucceededAsync(WorkItem resource, WriteOperationKind writeOperation, CancellationToken cancellationToken)
    {
        if (writeOperation is not WriteOperationKind.DeleteResource)
        {
            string statement = $"Update \"WorkItems\" SET \"Description\" = '{resource.Description}{Suffix}' WHERE \"Id\" = '{resource.StringId}'";
            await _dbContext.Database.ExecuteSqlRawAsync(statement, cancellationToken);
        }
    }
}
