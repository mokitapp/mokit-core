namespace Mokit.Infrastructure.Persistence.Transactions;

/// <summary>
/// Interface for centrally managing transactions
/// </summary>
public interface ITransactionManager
{
    /// <summary>
    /// Executes the specified operation within a transaction and returns the result.
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the specified operation within a transaction.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if there is an active transaction
    /// </summary>
    bool IsInTransaction();

    /// <summary>
    /// Starts a new transaction scope.
    /// </summary>
    Task<IDisposable> BeginTransactionScopeAsync(CancellationToken cancellationToken = default);
}
