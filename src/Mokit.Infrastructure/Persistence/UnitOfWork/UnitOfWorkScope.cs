using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Mokit.Application.Interfaces.Persistence;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Persistence.UnitOfWork;

/// <summary>
/// Represents an isolated unit of work scope with its own DbContext instance.
/// Thread-safe for Blazor Server - each scope has its own context.
/// </summary>
public class UnitOfWorkScope : IUnitOfWorkScope<MokitDbContext>
{
    private readonly MokitDbContext _context;
    private readonly IDbContextTransaction? _transaction;
    private readonly ILogger<UnitOfWorkScope> _logger;
    private bool _disposed;
    private bool _committed;

    public UnitOfWorkScope(
        MokitDbContext context,
        IDbContextTransaction? transaction,
        ILogger<UnitOfWorkScope> logger)
    {
        _context = context;
        _transaction = transaction;
        _logger = logger;
    }

    /// <summary>
    /// Gets the DbContext for this scope
    /// </summary>
    public MokitDbContext Context => _context;

    /// <summary>
    /// Saves changes and commits the transaction
    /// </summary>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UnitOfWorkScope));

        if (_committed)
            throw new InvalidOperationException("Scope has already been committed");

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
                _logger.LogDebug("Transaction committed successfully");
            }
            
            _committed = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing unit of work scope");
            
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                _logger.LogDebug("Transaction rolled back due to error");
            }
            
            throw;
        }
    }

    /// <summary>
    /// Rolls back the transaction
    /// </summary>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UnitOfWorkScope));

        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            _logger.LogDebug("Transaction rolled back");
        }
    }

    /// <summary>
    /// Saves changes without committing transaction
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UnitOfWorkScope));

        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        try
        {
            if (_transaction != null)
            {
                if (!_committed)
                {
                    _logger.LogDebug("Disposing uncommitted scope - rolling back");
                    await _transaction.RollbackAsync();
                }
                await _transaction.DisposeAsync();
            }

            await _context.DisposeAsync();
        }
        finally
        {
            _disposed = true;
        }
    }
}
