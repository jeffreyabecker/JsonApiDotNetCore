using System.Data.Common;
using DapperExample.TranslationToSql.DataModel;
using JsonApiDotNetCore;
using JsonApiDotNetCore.AtomicOperations;
using JsonApiDotNetCore.Configuration;

namespace DapperExample.AtomicOperations;

/// <summary>
/// Provides transaction support for atomic:operation requests using ADO.NET.
/// </summary>
public sealed class DapperTransactionFactory : IOperationsTransactionFactory
{
    private readonly IJsonApiOptions _options;
    private readonly IDataModelService _dataModelService;

    internal DapperTransaction? AmbientTransaction { get; private set; }

    public DapperTransactionFactory(IJsonApiOptions options, IDataModelService dataModelService)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(dataModelService);

        _options = options;
        _dataModelService = dataModelService;
    }

    internal async Task<DapperTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var instance = (IOperationsTransactionFactory)this;

        IOperationsTransaction transaction = await instance.BeginTransactionAsync(cancellationToken);
        return (DapperTransaction)transaction;
    }

    async Task<IOperationsTransaction> IOperationsTransactionFactory.BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (AmbientTransaction != null)
        {
            throw new InvalidOperationException("Cannot start transaction because another transaction is already active.");
        }

        DbConnection dbConnection = _dataModelService.CreateConnection();
        await dbConnection.OpenAsync(cancellationToken);

        DbTransaction transaction = _options.TransactionIsolationLevel != null
            ? await dbConnection.BeginTransactionAsync(_options.TransactionIsolationLevel.Value, cancellationToken)
            : await dbConnection.BeginTransactionAsync(cancellationToken);

        var transactionId = Guid.NewGuid();
        AmbientTransaction = new DapperTransaction(this, transaction, transactionId);

        return AmbientTransaction;
    }

    internal void Detach(DapperTransaction dapperTransaction)
    {
        if (AmbientTransaction != null && AmbientTransaction == dapperTransaction)
        {
            AmbientTransaction = null;
        }
        else
        {
            throw new InvalidOperationException("Failed to detach ambient transaction.");
        }
    }
}
