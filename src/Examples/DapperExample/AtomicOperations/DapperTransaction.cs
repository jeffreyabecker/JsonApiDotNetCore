using JsonApiDotNetCore;
using JsonApiDotNetCore.AtomicOperations;
using Npgsql;

namespace DapperExample.AtomicOperations;

/// <summary>
/// Represents an ADO.NET transaction in an atomic:operations request.
/// </summary>
public sealed class DapperTransaction : IOperationsTransaction
{
    private readonly DapperTransactionFactory _owner;

    internal NpgsqlTransaction Current { get; }

    /// <inheritdoc />
    public string TransactionId { get; }

    internal DapperTransaction(DapperTransactionFactory owner, NpgsqlTransaction current, Guid transactionId)
    {
        ArgumentGuard.NotNull(owner);
        ArgumentGuard.NotNull(current);

        _owner = owner;
        Current = current;
        TransactionId = transactionId.ToString();
    }

    /// <inheritdoc />
    public Task BeforeProcessOperationAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AfterProcessOperationAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CommitAsync(CancellationToken cancellationToken)
    {
        return Current.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection? connection = Current.Connection;

        await Current.DisposeAsync();

        if (connection != null)
        {
            await connection.DisposeAsync();
        }

        _owner.Detach(this);
    }
}
