using System.Data.Common;
using Dapper;
using DapperExample.AtomicOperations;
using DapperExample.TranslationToSql;
using DapperExample.TranslationToSql.Builders;
using DapperExample.TranslationToSql.DataModel;
using DapperExample.TranslationToSql.Transformations;
using DapperExample.TranslationToSql.TreeNodes;
using JsonApiDotNetCore;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Middleware;
using JsonApiDotNetCore.Queries;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Repositories;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;

namespace DapperExample.Repositories;

public sealed class DapperRepository<TResource, TId> : IResourceRepository<TResource, TId>, IRepositorySupportsTransaction
    where TResource : class, IIdentifiable<TId>
{
    private readonly ITargetedFields _targetedFields;
    private readonly IResourceGraph _resourceGraph;
    private readonly IResourceFactory _resourceFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IResourceDefinitionAccessor _resourceDefinitionAccessor;
    private readonly DapperTransactionFactory _transactionFactory;
    private readonly IDataModelService _dataModelService;
    private readonly SqlCaptureStore _captureStore;
    private readonly ILogger<DapperRepository<TResource, TId>> _logger;
    private readonly CollectionConverter _collectionConverter = new();
    private readonly ParameterFormatter _parameterFormatter = new();
    private readonly QueryLayerPaginationConverter _paginationConverter;
    private readonly DapperFacade _dapperFacade;

    private ResourceType ResourceType => _resourceGraph.GetResourceType<TResource>();

    public string? TransactionId => _transactionFactory.AmbientTransaction?.TransactionId;

    public DapperRepository(IJsonApiRequest request, ITargetedFields targetedFields, IResourceGraph resourceGraph, IResourceFactory resourceFactory,
        IResourceDefinitionAccessor resourceDefinitionAccessor, DapperTransactionFactory transactionFactory, IDataModelService dataModelService,
        SqlCaptureStore captureStore, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(request);
        ArgumentGuard.NotNull(targetedFields);
        ArgumentGuard.NotNull(resourceGraph);
        ArgumentGuard.NotNull(resourceFactory);
        ArgumentGuard.NotNull(resourceDefinitionAccessor);
        ArgumentGuard.NotNull(transactionFactory);
        ArgumentGuard.NotNull(dataModelService);
        ArgumentGuard.NotNull(captureStore);
        ArgumentGuard.NotNull(loggerFactory);

        _targetedFields = targetedFields;
        _resourceGraph = resourceGraph;
        _resourceFactory = resourceFactory;
        _loggerFactory = loggerFactory;
        _resourceDefinitionAccessor = resourceDefinitionAccessor;
        _transactionFactory = transactionFactory;
        _dataModelService = dataModelService;
        _captureStore = captureStore;
        _logger = loggerFactory.CreateLogger<DapperRepository<TResource, TId>>();
        _paginationConverter = new QueryLayerPaginationConverter(request);
        _dapperFacade = new DapperFacade(dataModelService);
    }

    public async Task<IReadOnlyCollection<TResource>> GetAsync(QueryLayer queryLayer, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(queryLayer);

        _paginationConverter.EnsureAtMostOnePagination(queryLayer);

        var mapper = new ResultSetMapper<TResource, TId>(queryLayer.Include);

        var selectBuilder = new SelectStatementBuilder(_dataModelService, _loggerFactory);
        SelectNode selectNode = selectBuilder.Build(queryLayer, SelectShape.Columns);
        CommandDefinition sqlCommand = _dapperFacade.GetSqlCommand(selectNode, cancellationToken);
        LogSqlCommand(sqlCommand);

        IReadOnlyCollection<TResource> resources = await ExecuteQueryAsync(async connection =>
        {
            // Reads must occur within the active transaction, when in an atomic:operations request.
            sqlCommand = sqlCommand.Associate(_transactionFactory.AmbientTransaction);

            // Unfortunately, there's no CancellationToken support. See https://github.com/DapperLib/Dapper/issues/1181.
            _ = await connection.QueryAsync(sqlCommand.CommandText, mapper.ResourceClrTypes, mapper.Map, sqlCommand.Parameters, sqlCommand.Transaction);

            return mapper.GetResources();
        }, cancellationToken);

        return resources;
    }

    public async Task<int> CountAsync(FilterExpression? filter, CancellationToken cancellationToken)
    {
        var queryLayer = new QueryLayer(ResourceType)
        {
            Filter = filter
        };

        var selectBuilder = new SelectStatementBuilder(_dataModelService, _loggerFactory);
        SelectNode selectNode = selectBuilder.Build(queryLayer, SelectShape.Count);
        CommandDefinition sqlCommand = _dapperFacade.GetSqlCommand(selectNode, cancellationToken);
        LogSqlCommand(sqlCommand);

        return await ExecuteQueryAsync(async connection => await connection.ExecuteScalarAsync<int>(sqlCommand), cancellationToken);
    }

    public Task<TResource> GetForCreateAsync(Type resourceClrType, TId id, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(resourceClrType);

        var resource = (TResource)_resourceFactory.CreateInstance(resourceClrType);
        resource.Id = id;

        return Task.FromResult(resource);
    }

    public async Task CreateAsync(TResource resourceFromRequest, TResource resourceForDatabase, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(resourceFromRequest);
        ArgumentGuard.NotNull(resourceForDatabase);

        var changeDetector = new ResourceChangeDetector(ResourceType, _dataModelService);

        await ApplyTargetedFieldsAsync(resourceFromRequest, resourceForDatabase, WriteOperationKind.CreateResource, cancellationToken);

        await _resourceDefinitionAccessor.OnWritingAsync(resourceForDatabase, WriteOperationKind.CreateResource, cancellationToken);

        changeDetector.CaptureNewValues(resourceForDatabase);

        IReadOnlyCollection<CommandDefinition> preSqlCommands =
            _dapperFacade.BuildSqlCommandsForOneToOneRelationshipsChangedToNotNull(changeDetector, cancellationToken);

        CommandDefinition insertCommand = _dapperFacade.BuildSqlCommandForCreate(changeDetector, cancellationToken);

        await ExecuteInTransactionAsync(async transaction =>
        {
            foreach (CommandDefinition sqlCommand in preSqlCommands)
            {
                LogSqlCommand(sqlCommand);
                int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand.Associate(transaction));

                if (rowsAffected > 1)
                {
                    throw new DataStoreUpdateException(new Exception("Multiple rows found."));
                }
            }

            LogSqlCommand(insertCommand);
            resourceForDatabase.Id = await transaction.Connection.ExecuteScalarAsync<TId>(insertCommand.Associate(transaction));

            IReadOnlyCollection<CommandDefinition> postSqlCommands =
                _dapperFacade.BuildSqlCommandsForChangedRelationshipsHavingForeignKeyAtRightSide(changeDetector, resourceForDatabase.Id, cancellationToken);

            foreach (CommandDefinition sqlCommand in postSqlCommands)
            {
                LogSqlCommand(sqlCommand);
                int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand.Associate(transaction));

                if (rowsAffected == 0)
                {
                    throw new DataStoreUpdateException(new Exception("Row does not exist."));
                }
            }
        }, cancellationToken);

        await _resourceDefinitionAccessor.OnWriteSucceededAsync(resourceForDatabase, WriteOperationKind.CreateResource, cancellationToken);
    }

    private async Task ApplyTargetedFieldsAsync(TResource resourceFromRequest, TResource resourceInDatabase, WriteOperationKind writeOperation,
        CancellationToken cancellationToken)
    {
        foreach (RelationshipAttribute relationship in _targetedFields.Relationships)
        {
            object? rightValue = relationship.GetValue(resourceFromRequest);
            object? rightValueEvaluated = await VisitSetRelationshipAsync(resourceInDatabase, relationship, rightValue, writeOperation, cancellationToken);

            relationship.SetValue(resourceInDatabase, rightValueEvaluated);
        }

        foreach (AttrAttribute attribute in _targetedFields.Attributes)
        {
            attribute.SetValue(resourceInDatabase, attribute.GetValue(resourceFromRequest));
        }
    }

    private async Task<object?> VisitSetRelationshipAsync(TResource leftResource, RelationshipAttribute relationship, object? rightValue,
        WriteOperationKind writeOperation, CancellationToken cancellationToken)
    {
        if (relationship is HasOneAttribute hasOneRelationship)
        {
            return await _resourceDefinitionAccessor.OnSetToOneRelationshipAsync(leftResource, hasOneRelationship, (IIdentifiable?)rightValue, writeOperation,
                cancellationToken);
        }

        if (relationship is HasManyAttribute hasManyRelationship)
        {
            HashSet<IIdentifiable> rightResourceIds = _collectionConverter.ExtractResources(rightValue).ToHashSet(IdentifiableComparer.Instance);

            await _resourceDefinitionAccessor.OnSetToManyRelationshipAsync(leftResource, hasManyRelationship, rightResourceIds, writeOperation,
                cancellationToken);

            return _collectionConverter.CopyToTypedCollection(rightResourceIds, relationship.Property.PropertyType);
        }

        return rightValue;
    }

    public async Task<TResource?> GetForUpdateAsync(QueryLayer queryLayer, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(queryLayer);

        IReadOnlyCollection<TResource> resources = await GetAsync(queryLayer, cancellationToken);
        return resources.FirstOrDefault();
    }

    public async Task UpdateAsync(TResource resourceFromRequest, TResource resourceFromDatabase, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(resourceFromRequest);
        ArgumentGuard.NotNull(resourceFromDatabase);

        var changeDetector = new ResourceChangeDetector(ResourceType, _dataModelService);
        changeDetector.CaptureCurrentValues(resourceFromDatabase);

        await ApplyTargetedFieldsAsync(resourceFromRequest, resourceFromDatabase, WriteOperationKind.UpdateResource, cancellationToken);

        await _resourceDefinitionAccessor.OnWritingAsync(resourceFromDatabase, WriteOperationKind.UpdateResource, cancellationToken);

        changeDetector.CaptureNewValues(resourceFromDatabase);
        changeDetector.AssertIsNotClearingAnyRequiredToOneRelationships(ResourceType.PublicName);

        IReadOnlyCollection<CommandDefinition> preSqlCommands =
            _dapperFacade.BuildSqlCommandsForOneToOneRelationshipsChangedToNotNull(changeDetector, cancellationToken);

        CommandDefinition? updateCommand = _dapperFacade.BuildSqlCommandForUpdate(changeDetector, resourceFromDatabase.Id, cancellationToken);

        IReadOnlyCollection<CommandDefinition> postSqlCommands =
            _dapperFacade.BuildSqlCommandsForChangedRelationshipsHavingForeignKeyAtRightSide(changeDetector, resourceFromDatabase.Id, cancellationToken);

        if (preSqlCommands.Any() || updateCommand != null || postSqlCommands.Any())
        {
            await ExecuteInTransactionAsync(async transaction =>
            {
                foreach (CommandDefinition sqlCommand in preSqlCommands)
                {
                    LogSqlCommand(sqlCommand);
                    int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand.Associate(transaction));

                    if (rowsAffected > 1)
                    {
                        throw new DataStoreUpdateException(new Exception("Multiple rows found."));
                    }
                }

                if (updateCommand != null)
                {
                    LogSqlCommand(updateCommand.Value);
                    int rowsAffected = await transaction.Connection.ExecuteAsync(updateCommand.Value.Associate(transaction));

                    if (rowsAffected != 1)
                    {
                        throw new DataStoreUpdateException(new Exception("Row does not exist or multiple rows found."));
                    }
                }

                foreach (CommandDefinition sqlCommand in postSqlCommands)
                {
                    LogSqlCommand(sqlCommand);
                    int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand.Associate(transaction));

                    if (rowsAffected == 0)
                    {
                        throw new DataStoreUpdateException(new Exception("Row does not exist."));
                    }
                }
            }, cancellationToken);

            await _resourceDefinitionAccessor.OnWriteSucceededAsync(resourceFromDatabase, WriteOperationKind.UpdateResource, cancellationToken);
        }
    }

    public async Task DeleteAsync(TResource? resourceFromDatabase, TId id, CancellationToken cancellationToken)
    {
        TResource placeholderResource = resourceFromDatabase ?? _resourceFactory.CreateInstance<TResource>();
        placeholderResource.Id = id;

        await _resourceDefinitionAccessor.OnWritingAsync(placeholderResource, WriteOperationKind.DeleteResource, cancellationToken);

        var deleteBuilder = new DeleteResourceStatementBuilder(_dataModelService);
        DeleteNode deleteNode = deleteBuilder.Build(ResourceType, placeholderResource.Id!);
        CommandDefinition sqlCommand = _dapperFacade.GetSqlCommand(deleteNode, cancellationToken);

        await ExecuteInTransactionAsync(async transaction =>
        {
            LogSqlCommand(sqlCommand);
            int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand.Associate(transaction));

            if (rowsAffected != 1)
            {
                throw new DataStoreUpdateException(new Exception("Row does not exist or multiple rows found."));
            }
        }, cancellationToken);

        await _resourceDefinitionAccessor.OnWriteSucceededAsync(placeholderResource, WriteOperationKind.DeleteResource, cancellationToken);
    }

    public async Task SetRelationshipAsync(TResource leftResource, object? rightValue, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(leftResource);

        RelationshipAttribute relationship = _targetedFields.Relationships.Single();

        var changeDetector = new ResourceChangeDetector(ResourceType, _dataModelService);
        changeDetector.CaptureCurrentValues(leftResource);

        object? rightValueEvaluated =
            await VisitSetRelationshipAsync(leftResource, relationship, rightValue, WriteOperationKind.SetRelationship, cancellationToken);

        relationship.SetValue(leftResource, rightValueEvaluated);

        await _resourceDefinitionAccessor.OnWritingAsync(leftResource, WriteOperationKind.SetRelationship, cancellationToken);

        changeDetector.CaptureNewValues(leftResource);
        changeDetector.AssertIsNotClearingAnyRequiredToOneRelationships(ResourceType.PublicName);

        IReadOnlyCollection<CommandDefinition> preSqlCommands =
            _dapperFacade.BuildSqlCommandsForOneToOneRelationshipsChangedToNotNull(changeDetector, cancellationToken);

        CommandDefinition? updateCommand = _dapperFacade.BuildSqlCommandForUpdate(changeDetector, leftResource.Id, cancellationToken);

        IReadOnlyCollection<CommandDefinition> postSqlCommands =
            _dapperFacade.BuildSqlCommandsForChangedRelationshipsHavingForeignKeyAtRightSide(changeDetector, leftResource.Id, cancellationToken);

        if (preSqlCommands.Any() || updateCommand != null || postSqlCommands.Any())
        {
            await ExecuteInTransactionAsync(async transaction =>
            {
                foreach (CommandDefinition sqlCommand in preSqlCommands)
                {
                    LogSqlCommand(sqlCommand);
                    int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand.Associate(transaction));

                    if (rowsAffected > 1)
                    {
                        throw new DataStoreUpdateException(new Exception("Multiple rows found."));
                    }
                }

                if (updateCommand != null)
                {
                    LogSqlCommand(updateCommand.Value);
                    int rowsAffected = await transaction.Connection.ExecuteAsync(updateCommand.Value.Associate(transaction));

                    if (rowsAffected != 1)
                    {
                        throw new DataStoreUpdateException(new Exception("Row does not exist or multiple rows found."));
                    }
                }

                foreach (CommandDefinition sqlCommand in postSqlCommands)
                {
                    LogSqlCommand(sqlCommand);
                    int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand.Associate(transaction));

                    if (rowsAffected == 0)
                    {
                        throw new DataStoreUpdateException(new Exception("Row does not exist."));
                    }
                }
            }, cancellationToken);

            await _resourceDefinitionAccessor.OnWriteSucceededAsync(leftResource, WriteOperationKind.SetRelationship, cancellationToken);
        }
    }

    public async Task AddToToManyRelationshipAsync(TResource? leftResource, TId leftId, ISet<IIdentifiable> rightResourceIds,
        CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(rightResourceIds);

        var relationship = (HasManyAttribute)_targetedFields.Relationships.Single();

        TResource leftPlaceholderResource = leftResource ?? _resourceFactory.CreateInstance<TResource>();
        leftPlaceholderResource.Id = leftId;

        await _resourceDefinitionAccessor.OnAddToRelationshipAsync(leftPlaceholderResource, relationship, rightResourceIds, cancellationToken);
        relationship.SetValue(leftPlaceholderResource, _collectionConverter.CopyToTypedCollection(rightResourceIds, relationship.Property.PropertyType));

        await _resourceDefinitionAccessor.OnWritingAsync(leftPlaceholderResource, WriteOperationKind.AddToRelationship, cancellationToken);

        if (rightResourceIds.Any())
        {
            RelationshipForeignKey foreignKey = _dataModelService.GetForeignKey(relationship);
            object[] rightResourceIdValues = rightResourceIds.Select(resource => resource.GetTypedId()).ToArray();

            CommandDefinition sqlCommand =
                _dapperFacade.BuildSqlCommandForAddToToMany(foreignKey, leftPlaceholderResource.Id!, rightResourceIdValues, cancellationToken);

            await ExecuteInTransactionAsync(async transaction =>
            {
                LogSqlCommand(sqlCommand);
                int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand.Associate(transaction));

                if (rowsAffected != rightResourceIdValues.Length)
                {
                    throw new DataStoreUpdateException(new Exception("Row does not exist or multiple rows found."));
                }
            }, cancellationToken);

            await _resourceDefinitionAccessor.OnWriteSucceededAsync(leftPlaceholderResource, WriteOperationKind.AddToRelationship, cancellationToken);
        }
    }

    public async Task RemoveFromToManyRelationshipAsync(TResource leftResource, ISet<IIdentifiable> rightResourceIds, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(leftResource);
        ArgumentGuard.NotNull(rightResourceIds);

        var relationship = (HasManyAttribute)_targetedFields.Relationships.Single();

        await _resourceDefinitionAccessor.OnRemoveFromRelationshipAsync(leftResource, relationship, rightResourceIds, cancellationToken);
        relationship.SetValue(leftResource, _collectionConverter.CopyToTypedCollection(rightResourceIds, relationship.Property.PropertyType));

        await _resourceDefinitionAccessor.OnWritingAsync(leftResource, WriteOperationKind.RemoveFromRelationship, cancellationToken);

        if (rightResourceIds.Any())
        {
            RelationshipForeignKey foreignKey = _dataModelService.GetForeignKey(relationship);
            object[] rightResourceIdValues = rightResourceIds.Select(resource => resource.GetTypedId()).ToArray();
            CommandDefinition sqlCommand = _dapperFacade.BuildSqlCommandForRemoveFromToMany(foreignKey, rightResourceIdValues, cancellationToken);

            await ExecuteInTransactionAsync(async transaction =>
            {
                LogSqlCommand(sqlCommand);
                int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand.Associate(transaction));

                if (rowsAffected != rightResourceIdValues.Length)
                {
                    throw new DataStoreUpdateException(new Exception("Row does not exist or multiple rows found."));
                }
            }, cancellationToken);

            await _resourceDefinitionAccessor.OnWriteSucceededAsync(leftResource, WriteOperationKind.RemoveFromRelationship, cancellationToken);
        }
    }

    private void LogSqlCommand(CommandDefinition command)
    {
        var parameters = (IDictionary<string, object?>?)command.Parameters;

        _captureStore.Add(command.CommandText, parameters);

        string message = GetLogText(command.CommandText, parameters);
        _logger.LogInformation(message);
    }

    private string GetLogText(string statement, IDictionary<string, object?>? parameters)
    {
        if (parameters?.Any() == true)
        {
            string parametersText = string.Join(", ", parameters.Select(parameter => _parameterFormatter.Format(parameter.Key, parameter.Value)));
            return $"Executing SQL with parameters: {parametersText}{Environment.NewLine}{statement}";
        }

        return $"Executing SQL: {Environment.NewLine}{statement}";
    }

    private async Task<TResult> ExecuteQueryAsync<TResult>(Func<DbConnection, Task<TResult>> asyncAction, CancellationToken cancellationToken)
    {
        if (_transactionFactory.AmbientTransaction != null)
        {
            DbConnection connection = _transactionFactory.AmbientTransaction.Current.Connection!;
            return await asyncAction(connection);
        }

        await using DbConnection dbConnection = _dataModelService.CreateConnection();
        await dbConnection.OpenAsync(cancellationToken);

        return await asyncAction(dbConnection);
    }

    private async Task ExecuteInTransactionAsync(Func<DbTransaction, Task> asyncAction, CancellationToken cancellationToken)
    {
        try
        {
            if (_transactionFactory.AmbientTransaction != null)
            {
                await asyncAction(_transactionFactory.AmbientTransaction.Current);
            }
            else
            {
                await using DapperTransaction transaction = await _transactionFactory.BeginTransactionAsync(cancellationToken);

                await asyncAction(transaction.Current);

                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbException exception)
        {
            throw new DataStoreUpdateException(exception);
        }
    }
}
