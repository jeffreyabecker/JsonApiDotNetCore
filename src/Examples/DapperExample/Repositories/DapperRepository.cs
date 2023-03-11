using System.Data.Common;
using System.Text;
using Dapper;
using DapperExample.AtomicOperations;
using DapperExample.TranslationToSql;
using DapperExample.TranslationToSql.Builders;
using DapperExample.TranslationToSql.DataModel;
using DapperExample.TranslationToSql.TreeNodes;
using JsonApiDotNetCore;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Middleware;
using JsonApiDotNetCore.Queries;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.Repositories;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;
using Npgsql;

namespace DapperExample.Repositories;

// TODO: Document limitations
// - Working SQL, but not very optimal (magnitudes slower than EF Core)
// - No many-to-many
// - No resource inheritance
// - No composite primary/foreign keys
// - No nested pagination?
// - No eager loading?
// - No resource constructor injection (materialization by Dapper)
// - Simplified change detection: includes scalar properties, but relationships only one level deep
// - Mapping of table/column/key names based on hardcoded conventions
// - Self-referencing resources and relationship cycles
// - No support for IResourceDefinition.OnRegisterQueryableHandlersForQueryStringParameters(), obviously

public sealed class DapperRepository<TResource, TId> : IResourceRepository<TResource, TId>, IRepositorySupportsTransaction
    where TResource : class, IIdentifiable<TId>
{
    private readonly CollectionConverter _collectionConverter = new();
    private readonly ParameterFormatter _parameterFormatter = new();

    private readonly string _connectionString;
    private readonly ITargetedFields _targetedFields;
    private readonly IResourceGraph _resourceGraph;
    private readonly IResourceFactory _resourceFactory;
    private readonly ILogger<DapperRepository<TResource, TId>> _logger;
    private readonly IResourceDefinitionAccessor _resourceDefinitionAccessor;
    private readonly IJsonApiOptions _options;
    private readonly DapperTransactionFactory _transactionFactory;
    private readonly IDataModelService _dataModelService;
    private readonly SqlCaptureStore _captureStore;

    private ResourceType ResourceType => _resourceGraph.GetResourceType<TResource>();

    public string? TransactionId => _transactionFactory.AmbientTransaction?.TransactionId;

    public DapperRepository(ITargetedFields targetedFields, IResourceGraph resourceGraph, IResourceFactory resourceFactory, ILoggerFactory loggerFactory,
        IResourceDefinitionAccessor resourceDefinitionAccessor, IJsonApiOptions options, DapperTransactionFactory transactionFactory,
        IDataModelService dataModelService, SqlCaptureStore captureStore)
    {
        ArgumentGuard.NotNull(targetedFields);
        ArgumentGuard.NotNull(resourceGraph);
        ArgumentGuard.NotNull(resourceFactory);
        ArgumentGuard.NotNull(loggerFactory);
        ArgumentGuard.NotNull(resourceDefinitionAccessor);
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(transactionFactory);
        ArgumentGuard.NotNull(dataModelService);
        ArgumentGuard.NotNull(captureStore);

        _connectionString = dataModelService.GetConnectionString();
        _targetedFields = targetedFields;
        _resourceGraph = resourceGraph;
        _resourceFactory = resourceFactory;
        _logger = loggerFactory.CreateLogger<DapperRepository<TResource, TId>>();
        _resourceDefinitionAccessor = resourceDefinitionAccessor;
        _options = options;
        _transactionFactory = transactionFactory;
        _dataModelService = dataModelService;
        _captureStore = captureStore;
    }

    public async Task<IReadOnlyCollection<TResource>> GetAsync(QueryLayer queryLayer, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(queryLayer);

        var selectBuilder = new SelectStatementBuilder(_dataModelService);
        SelectNode selectNode = selectBuilder.Build(queryLayer, SelectShape.Columns);
        CommandDefinition sqlCommand = GetSqlCommand(selectNode, cancellationToken);
        LogSqlCommand(sqlCommand);

        string splitOn = GetSplitOnColumns(selectNode);
        var mapper = new ResultSetMapper<TResource, TId>(queryLayer.Include);

        IReadOnlyCollection<TResource> resources = await ExecuteQueryAsync(async connection =>
        {
            // https://github.com/DapperLib/Dapper/issues/1181
            _ = await connection.QueryAsync(sqlCommand.CommandText, mapper.ResourceClrTypes, mapper.Map, sqlCommand.Parameters, splitOn: splitOn);

            return mapper.GetResources();
        }, cancellationToken);

        return resources;
    }

    private static string GetSplitOnColumns(SelectNode selectNode)
    {
        var splitOnBuilder = new StringBuilder();

        foreach (TableSourceNode tableSource in selectNode.Selectors.Where(pair => pair.Value.Any()).Select(pair => pair.Key.TableSource).Skip(1))
        {
            if (splitOnBuilder.Length > 0)
            {
                splitOnBuilder.Append(',');
            }

            splitOnBuilder.Append($"{tableSource.Alias}_SplitId");
        }

        return splitOnBuilder.ToString();
    }

    public async Task<int> CountAsync(FilterExpression? filter, CancellationToken cancellationToken)
    {
        var queryLayer = new QueryLayer(ResourceType)
        {
            Filter = filter
        };

        var selectBuilder = new SelectStatementBuilder(_dataModelService);
        SelectNode selectNode = selectBuilder.Build(queryLayer, SelectShape.Count);
        CommandDefinition sqlCommand = GetSqlCommand(selectNode, cancellationToken);
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

        IReadOnlyCollection<CommandDefinition> preSqlCommands = BuildSqlCommandsForChangedOneToOneRelationshipsToNotNull(changeDetector, cancellationToken);

        CommandDefinition insertCommand = BuildSqlCommandForCreate(changeDetector, cancellationToken);

        await ExecuteInTransactionAsync(async transaction =>
        {
            foreach (CommandDefinition sqlCommand in preSqlCommands)
            {
                LogSqlCommand(sqlCommand);
                int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand);

                if (rowsAffected > 1)
                {
                    throw new DataStoreUpdateException(new Exception("Multiple rows found."));
                }
            }

            LogSqlCommand(insertCommand);
            resourceForDatabase.Id = await transaction.Connection.ExecuteScalarAsync<TId>(insertCommand);

            IReadOnlyCollection<CommandDefinition> postSqlCommands =
                BuildSqlCommandsForChangedRelationshipsHavingForeignKeyAtRightSide(changeDetector, resourceForDatabase.Id, cancellationToken);

            foreach (CommandDefinition sqlCommand in postSqlCommands)
            {
                LogSqlCommand(sqlCommand);
                int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand);

                if (rowsAffected == 0)
                {
                    throw new DataStoreUpdateException(new Exception("Row does not exist."));
                }
            }
        }, cancellationToken);

        await _resourceDefinitionAccessor.OnWriteSucceededAsync(resourceForDatabase, WriteOperationKind.CreateResource, cancellationToken);
    }

    private CommandDefinition BuildSqlCommandForCreate(ResourceChangeDetector changeDetector, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, object?> columnsToSet = changeDetector.GetChangedColumnValues();

        var insertBuilder = new InsertStatementBuilder(_dataModelService);
        InsertNode insertNode = insertBuilder.Build(ResourceType, columnsToSet);
        return GetSqlCommand(insertNode, cancellationToken);
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

        IReadOnlyCollection<CommandDefinition> preSqlCommands = BuildSqlCommandsForChangedOneToOneRelationshipsToNotNull(changeDetector, cancellationToken);

        CommandDefinition? updateCommand = BuildSqlCommandForUpdate(changeDetector, resourceFromDatabase.Id, cancellationToken);

        IReadOnlyCollection<CommandDefinition> postSqlCommands =
            BuildSqlCommandsForChangedRelationshipsHavingForeignKeyAtRightSide(changeDetector, resourceFromDatabase.Id, cancellationToken);

        if (preSqlCommands.Any() || updateCommand != null || postSqlCommands.Any())
        {
            await ExecuteInTransactionAsync(async transaction =>
            {
                foreach (CommandDefinition sqlCommand in preSqlCommands)
                {
                    LogSqlCommand(sqlCommand);
                    int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand);

                    if (rowsAffected > 1)
                    {
                        throw new DataStoreUpdateException(new Exception("Multiple rows found."));
                    }
                }

                if (updateCommand != null)
                {
                    LogSqlCommand(updateCommand.Value);
                    int rowsAffected = await transaction.Connection.ExecuteAsync(updateCommand.Value);

                    if (rowsAffected != 1)
                    {
                        throw new DataStoreUpdateException(new Exception("Row does not exist or multiple rows found."));
                    }
                }

                foreach (CommandDefinition sqlCommand in postSqlCommands)
                {
                    LogSqlCommand(sqlCommand);
                    int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand);

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
        CommandDefinition sqlCommand = GetSqlCommand(deleteNode, cancellationToken);

        await ExecuteInTransactionAsync(async transaction =>
        {
            LogSqlCommand(sqlCommand);
            int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand);

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

        IReadOnlyCollection<CommandDefinition> preSqlCommands = BuildSqlCommandsForChangedOneToOneRelationshipsToNotNull(changeDetector, cancellationToken);

        CommandDefinition? updateCommand = BuildSqlCommandForUpdate(changeDetector, leftResource.Id, cancellationToken);

        IReadOnlyCollection<CommandDefinition> postSqlCommands =
            BuildSqlCommandsForChangedRelationshipsHavingForeignKeyAtRightSide(changeDetector, leftResource.Id, cancellationToken);

        if (preSqlCommands.Any() || updateCommand != null || postSqlCommands.Any())
        {
            await ExecuteInTransactionAsync(async transaction =>
            {
                foreach (CommandDefinition sqlCommand in preSqlCommands)
                {
                    LogSqlCommand(sqlCommand);
                    int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand);

                    if (rowsAffected > 1)
                    {
                        throw new DataStoreUpdateException(new Exception("Multiple rows found."));
                    }
                }

                if (updateCommand != null)
                {
                    LogSqlCommand(updateCommand.Value);
                    int rowsAffected = await transaction.Connection.ExecuteAsync(updateCommand.Value);

                    if (rowsAffected != 1)
                    {
                        throw new DataStoreUpdateException(new Exception("Row does not exist or multiple rows found."));
                    }
                }

                foreach (CommandDefinition sqlCommand in postSqlCommands)
                {
                    LogSqlCommand(sqlCommand);
                    int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand);

                    if (rowsAffected == 0)
                    {
                        throw new DataStoreUpdateException(new Exception("Row does not exist."));
                    }
                }
            }, cancellationToken);

            await _resourceDefinitionAccessor.OnWriteSucceededAsync(leftResource, WriteOperationKind.SetRelationship, cancellationToken);
        }
    }

    private IReadOnlyCollection<CommandDefinition> BuildSqlCommandsForChangedOneToOneRelationshipsToNotNull(ResourceChangeDetector changeDetector,
        CancellationToken cancellationToken)
    {
        List<CommandDefinition> sqlCommands = new();

        foreach ((HasOneAttribute relationship, (object? currentRightId, object newRightId)) in changeDetector.GetChangedOneToOneRelationshipsToNotNull())
        {
            // To prevent a unique constraint violation on the foreign key, first detach/delete the other row pointing to us, if any.
            // See https://github.com/json-api-dotnet/JsonApiDotNetCore/issues/502.

            RelationshipForeignKey foreignKey = _dataModelService.GetForeignKey(relationship);

            ResourceType resourceType = foreignKey.IsAtLeftSide ? relationship.LeftType : relationship.RightType;
            string whereColumnName = foreignKey.IsAtLeftSide ? foreignKey.ColumnName : nameof(Identifiable<object>.Id);
            object? whereValue = foreignKey.IsAtLeftSide ? newRightId : currentRightId;

            if (whereValue == null)
            {
                // Add comment on this scenario.
                continue;
            }

            if (foreignKey.IsNullable)
            {
                var updateBuilder = new UpdateClearOneToOneStatementBuilder(_dataModelService);
                UpdateNode updateNode = updateBuilder.Build(resourceType, foreignKey.ColumnName, whereColumnName, whereValue);
                CommandDefinition sqlCommand = GetSqlCommand(updateNode, cancellationToken);
                sqlCommands.Add(sqlCommand);
            }
            else
            {
                var deleteBuilder = new DeleteOneToOneStatementBuilder(_dataModelService);
                DeleteNode deleteNode = deleteBuilder.Build(resourceType, whereColumnName, whereValue);
                CommandDefinition sqlCommand = GetSqlCommand(deleteNode, cancellationToken);
                sqlCommands.Add(sqlCommand);
            }
        }

        return sqlCommands;
    }

    private CommandDefinition? BuildSqlCommandForUpdate(ResourceChangeDetector changeDetector, TId leftId, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, object?> columnsToUpdate = changeDetector.GetChangedColumnValues();

        if (columnsToUpdate.Any())
        {
            var updateBuilder = new UpdateResourceStatementBuilder(_dataModelService);
            UpdateNode updateNode = updateBuilder.Build(ResourceType, columnsToUpdate, leftId!);
            return GetSqlCommand(updateNode, cancellationToken);
        }

        return null;
    }

    private IReadOnlyCollection<CommandDefinition> BuildSqlCommandsForChangedRelationshipsHavingForeignKeyAtRightSide(ResourceChangeDetector changeDetector,
        TId leftId, CancellationToken cancellationToken)
    {
        List<CommandDefinition> sqlCommands = new();

        foreach ((HasOneAttribute hasOneRelationship, (object? currentRightId, object? newRightId)) in changeDetector
            .GetChangedToOneRelationshipsWithForeignKeyAtRightSide())
        {
            RelationshipForeignKey foreignKey = _dataModelService.GetForeignKey(hasOneRelationship);

            var columnsToUpdate = new Dictionary<string, object?>
            {
                [foreignKey.ColumnName] = newRightId == null ? null : leftId
            };

            var updateBuilder = new UpdateResourceStatementBuilder(_dataModelService);
            UpdateNode updateNode = updateBuilder.Build(hasOneRelationship.RightType, columnsToUpdate, (newRightId ?? currentRightId)!);
            CommandDefinition sqlCommand = GetSqlCommand(updateNode, cancellationToken);
            sqlCommands.Add(sqlCommand);
        }

        foreach ((HasManyAttribute hasManyRelationship, (ISet<object> currentRightIds, ISet<object> newRightIds)) in changeDetector
            .GetChangedToManyRelationships())
        {
            RelationshipForeignKey foreignKey = _dataModelService.GetForeignKey(hasManyRelationship);

            object[] rightIdsToRemove = currentRightIds.Except(newRightIds).ToArray();
            object[] rightIdsToAdd = newRightIds.Except(currentRightIds).ToArray();

            if (rightIdsToRemove.Any())
            {
                CommandDefinition sqlCommand = BuildSqlCommandForRemoveFromToMany(foreignKey, rightIdsToRemove, cancellationToken);
                sqlCommands.Add(sqlCommand);
            }

            if (rightIdsToAdd.Any())
            {
                CommandDefinition sqlCommand = BuildSqlCommandForAddToToMany(foreignKey, leftId!, rightIdsToAdd, cancellationToken);
                sqlCommands.Add(sqlCommand);
            }
        }

        return sqlCommands;
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
            CommandDefinition sqlCommand = BuildSqlCommandForAddToToMany(foreignKey, leftPlaceholderResource.Id!, rightResourceIdValues, cancellationToken);

            await ExecuteInTransactionAsync(async transaction =>
            {
                LogSqlCommand(sqlCommand);
                int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand);

                if (rowsAffected != rightResourceIdValues.Length)
                {
                    throw new DataStoreUpdateException(new Exception("Row does not exist or multiple rows found."));
                }
            }, cancellationToken);

            await _resourceDefinitionAccessor.OnWriteSucceededAsync(leftPlaceholderResource, WriteOperationKind.AddToRelationship, cancellationToken);
        }
    }

    private CommandDefinition BuildSqlCommandForAddToToMany(RelationshipForeignKey foreignKey, object leftId, object[] rightResourceIdValues,
        CancellationToken cancellationToken)
    {
        var columnsToUpdate = new Dictionary<string, object?>
        {
            [foreignKey.ColumnName] = leftId
        };

        var updateBuilder = new UpdateResourceStatementBuilder(_dataModelService);
        UpdateNode updateNode = updateBuilder.Build(foreignKey.Relationship.RightType, columnsToUpdate, rightResourceIdValues);
        return GetSqlCommand(updateNode, cancellationToken);
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
            CommandDefinition sqlCommand = BuildSqlCommandForRemoveFromToMany(foreignKey, rightResourceIdValues, cancellationToken);

            await ExecuteInTransactionAsync(async transaction =>
            {
                LogSqlCommand(sqlCommand);
                int rowsAffected = await transaction.Connection.ExecuteAsync(sqlCommand);

                if (rowsAffected != rightResourceIdValues.Length)
                {
                    throw new DataStoreUpdateException(new Exception("Row does not exist or multiple rows found."));
                }
            }, cancellationToken);

            await _resourceDefinitionAccessor.OnWriteSucceededAsync(leftResource, WriteOperationKind.RemoveFromRelationship, cancellationToken);
        }
    }

    private CommandDefinition BuildSqlCommandForRemoveFromToMany(RelationshipForeignKey foreignKey, object[] rightResourceIdValues,
        CancellationToken cancellationToken)
    {
        if (!foreignKey.IsNullable)
        {
            var deleteBuilder = new DeleteResourceStatementBuilder(_dataModelService);
            DeleteNode deleteNode = deleteBuilder.Build(foreignKey.Relationship.RightType, rightResourceIdValues);
            return GetSqlCommand(deleteNode, cancellationToken);
        }

        var columnsToUpdate = new Dictionary<string, object?>
        {
            [foreignKey.ColumnName] = null
        };

        var updateBuilder = new UpdateResourceStatementBuilder(_dataModelService);
        UpdateNode updateNode = updateBuilder.Build(foreignKey.Relationship.RightType, columnsToUpdate, rightResourceIdValues);
        return GetSqlCommand(updateNode, cancellationToken);
    }

    private CommandDefinition GetSqlCommand(SqlTreeNode node, CancellationToken cancellationToken)
    {
        var queryBuilder = new SqlQueryBuilder();
        string statement = queryBuilder.GetCommand(node);
        IDictionary<string, object?> parameters = queryBuilder.Parameters;

        return new CommandDefinition(statement, parameters, cancellationToken: cancellationToken);
    }

    private void LogSqlCommand(CommandDefinition command)
    {
        var parameters = (IDictionary<string, object?>)command.Parameters;

        _captureStore.Add(command.CommandText, parameters);

        string message = GetLogText(command.CommandText, parameters);
        _logger.LogInformation(message);
    }

    private string GetLogText(string statement, IDictionary<string, object?> parameters)
    {
        if (parameters.Any())
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

        await using var dbConnection = new NpgsqlConnection(_connectionString);
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