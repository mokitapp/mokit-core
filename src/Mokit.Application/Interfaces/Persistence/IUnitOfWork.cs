namespace Mokit.Application.Interfaces.Persistence;

/// <summary>
/// Unit of Work pattern interface - Factory-based for Blazor Server compatibility
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Creates a new isolated scope with its own database context.
    /// Use this for all database operations to avoid concurrency issues in Blazor Server.
    /// </summary>
    Task<IUnitOfWorkScope> CreateScopeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new isolated scope with transaction support.
    /// NOTE: This method is DEPRECATED when using NpgsqlRetryingExecutionStrategy.
    /// Use ExecuteTransactionAsync instead for proper retry support.
    /// </summary>
    [Obsolete("Use ExecuteTransactionAsync for proper retry execution strategy support")]
    Task<IUnitOfWorkScope> CreateTransactionalScopeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation within a transaction with proper execution strategy support.
    /// The entire operation (including begin, execute, and commit) is wrapped in a single
    /// execution strategy call, which is required for NpgsqlRetryingExecutionStrategy.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the operation</typeparam>
    /// <param name="operation">The operation to execute within the transaction. 
    /// Receives the scope as parameter and should return the result.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the operation</returns>
    Task<TResult> ExecuteTransactionAsync<TResult>(
        Func<IUnitOfWorkScope, Task<TResult>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation within a transaction with proper execution strategy support (no return value).
    /// </summary>
    /// <param name="operation">The operation to execute within the transaction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteTransactionAsync(
        Func<IUnitOfWorkScope, Task> operation,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Typed Unit of Work for specific DbContext type
/// </summary>
public interface IUnitOfWork<TContext> : IUnitOfWork where TContext : class
{
    /// <summary>
    /// Creates a new typed scope with access to specific DbContext
    /// </summary>
    new Task<IUnitOfWorkScope<TContext>> CreateScopeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new typed scope with transaction support.
    /// NOTE: This method is DEPRECATED when using NpgsqlRetryingExecutionStrategy.
    /// Use ExecuteTransactionAsync instead for proper retry support.
    /// </summary>
    [Obsolete("Use ExecuteTransactionAsync for proper retry execution strategy support")]
    new Task<IUnitOfWorkScope<TContext>> CreateTransactionalScopeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation within a transaction with proper execution strategy support.
    /// The entire operation (including begin, execute, and commit) is wrapped in a single
    /// execution strategy call, which is required for NpgsqlRetryingExecutionStrategy.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the operation</typeparam>
    /// <param name="operation">The operation to execute within the transaction. 
    /// Receives the typed scope as parameter and should return the result.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of the operation</returns>
    Task<TResult> ExecuteTransactionAsync<TResult>(
        Func<IUnitOfWorkScope<TContext>, Task<TResult>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation within a transaction with proper execution strategy support (no return value).
    /// </summary>
    /// <param name="operation">The operation to execute within the transaction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteTransactionAsync(
        Func<IUnitOfWorkScope<TContext>, Task> operation,
        CancellationToken cancellationToken = default);
}

