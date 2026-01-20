namespace Mokit.Application.Interfaces.Persistence;

/// <summary>
/// Represents an isolated unit of work scope with its own database context.
/// Each scope creates a new context for thread-safe operations in Blazor Server.
/// </summary>
public interface IUnitOfWorkScope : IAsyncDisposable
{
    /// <summary>
    /// Saves changes and commits the transaction
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction without saving
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes without committing (for read operations that need tracking)
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Typed unit of work scope with access to specific DbContext type
/// </summary>
public interface IUnitOfWorkScope<TContext> : IUnitOfWorkScope where TContext : class
{
    /// <summary>
    /// Gets the typed DbContext instance for this scope
    /// </summary>
    TContext Context { get; }
}
