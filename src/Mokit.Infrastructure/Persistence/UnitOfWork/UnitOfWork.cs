using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mokit.Application.Interfaces.Persistence;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Persistence.UnitOfWork;

/// <summary>
/// Factory-based Unit of Work implementation for Blazor Server compatibility.
/// Creates isolated DbContext instances for each scope to avoid concurrency issues.
/// </summary>
public class UnitOfWork : IUnitOfWork<MokitDbContext>, IUnitOfWork
{
    private readonly IDbContextFactory<MokitDbContext> _contextFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(
        IDbContextFactory<MokitDbContext> contextFactory,
        ILoggerFactory loggerFactory,
        ILogger<UnitOfWork> logger)
    {
        _contextFactory = contextFactory;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new isolated scope with its own DbContext instance (no transaction)
    /// </summary>
    public async Task<IUnitOfWorkScope<MokitDbContext>> CreateScopeAsync(CancellationToken cancellationToken = default)
    {
        var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var scopeLogger = _loggerFactory.CreateLogger<UnitOfWorkScope>();
        
        _logger.LogDebug("Created new UnitOfWorkScope without transaction");
        
        return new UnitOfWorkScope(context, null, scopeLogger);
    }

    /// <summary>
    /// Creates a new isolated scope with transaction support.
    /// NOTE: This method is DEPRECATED when using NpgsqlRetryingExecutionStrategy.
    /// Use ExecuteTransactionAsync instead for proper retry support.
    /// </summary>
    [Obsolete("Use ExecuteTransactionAsync for proper retry execution strategy support")]
    public async Task<IUnitOfWorkScope<MokitDbContext>> CreateTransactionalScopeAsync(CancellationToken cancellationToken = default)
    {
        var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        // Use execution strategy for retry logic
        var strategy = context.Database.CreateExecutionStrategy();
        
        return await strategy.ExecuteAsync(async () =>
        {
            var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var scopeLogger = _loggerFactory.CreateLogger<UnitOfWorkScope>();
            
            _logger.LogDebug("Created new UnitOfWorkScope with transaction");
            
            return new UnitOfWorkScope(context, transaction, scopeLogger);
        });
    }

    /// <summary>
    /// Executes an operation within a transaction with proper execution strategy support.
    /// The entire operation (including begin, execute, and commit) is wrapped in a single
    /// execution strategy call, which is required for NpgsqlRetryingExecutionStrategy.
    /// </summary>
    public async Task<TResult> ExecuteTransactionAsync<TResult>(
        Func<IUnitOfWorkScope<MokitDbContext>, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            var scopeLogger = _loggerFactory.CreateLogger<UnitOfWorkScope>();
            
            _logger.LogDebug("Starting transactional operation with execution strategy");
            
            // Create a non-transactional scope since we manage transaction here
            await using var scope = new UnitOfWorkScope(context, null, scopeLogger);
            
            try
            {
                var result = await operation(scope);
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                _logger.LogDebug("Transactional operation completed successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Rolling back transaction due to error");
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    /// <summary>
    /// Executes an operation within a transaction with proper execution strategy support (no return value).
    /// </summary>
    public async Task ExecuteTransactionAsync(
        Func<IUnitOfWorkScope<MokitDbContext>, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteTransactionAsync<bool>(async scope =>
        {
            await operation(scope);
            return true;
        }, cancellationToken);
    }

    // Explicit interface implementations for non-generic IUnitOfWork
    async Task<IUnitOfWorkScope> IUnitOfWork.CreateScopeAsync(CancellationToken cancellationToken)
        => await CreateScopeAsync(cancellationToken);

    [Obsolete("Use ExecuteTransactionAsync for proper retry execution strategy support")]
    async Task<IUnitOfWorkScope> IUnitOfWork.CreateTransactionalScopeAsync(CancellationToken cancellationToken)
        => await CreateTransactionalScopeAsync(cancellationToken);

    async Task<TResult> IUnitOfWork.ExecuteTransactionAsync<TResult>(
        Func<IUnitOfWorkScope, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        return await ExecuteTransactionAsync(async scope =>
        {
            return await operation(scope);
        }, cancellationToken);
    }

    async Task IUnitOfWork.ExecuteTransactionAsync(
        Func<IUnitOfWorkScope, Task> operation,
        CancellationToken cancellationToken)
    {
        await ExecuteTransactionAsync(async scope =>
        {
            await operation(scope);
        }, cancellationToken);
    }
}
