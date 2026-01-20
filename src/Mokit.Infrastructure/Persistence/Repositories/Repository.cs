using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mokit.Application.Interfaces.Persistence;
using Mokit.Infrastructure.Data;
using System.Linq.Expressions;

namespace Mokit.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic repository implementation following Clean Architecture standards
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly MokitDbContext _context;
    protected readonly ILogger _logger;
    protected readonly DbSet<T> _dbSet;

    public Repository(MokitDbContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
        _dbSet = _context.Set<T>();
    }

    /// <summary>
    /// Gets all records
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets record by ID
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync([id], cancellationToken);
    }

    /// <summary>
    /// Finds single record
    /// </summary>
    public virtual async Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Finds records matching predicate
    /// </summary>
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds record
    /// </summary>
    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    /// <summary>
    /// Adds multiple records
    /// </summary>
    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    /// <summary>
    /// Updates record
    /// </summary>
    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    /// <summary>
    /// Updates record (asynchronous)
    /// </summary>
    public virtual async Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            _dbSet.Update(entity);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity");
            return false;
        }
    }

    /// <summary>
    /// Deletes record
    /// </summary>
    public virtual void Delete(T entity)
    {
        if (_context.Entry(entity).State == EntityState.Detached)
            _dbSet.Attach(entity);
        _dbSet.Remove(entity);
    }

    /// <summary>
    /// Removes record
    /// </summary>
    public virtual void Remove(T entity)
    {
        Delete(entity);
    }

    /// <summary>
    /// Removes multiple records
    /// </summary>
    public virtual void RemoveRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    /// <summary>
    /// Returns queryable
    /// </summary>
    public virtual IQueryable<T> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }

    /// <summary>
    /// Reloads entity
    /// </summary>
    public virtual async Task ReloadEntityAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _context.Entry(entity).ReloadAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if record exists
    /// </summary>
    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }

    /// <summary>
    /// Checks if record matching predicate exists
    /// </summary>
    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Gets record count
    /// </summary>
    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Gets count of records matching predicate
    /// </summary>
    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(predicate, cancellationToken);
    }

    // ========== IgnoreQueryFilters Methods (for Admin Panel) ==========

    /// <summary>
    /// Gets all records including soft-deleted ones
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetAllWithDeletedAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.IgnoreQueryFilters().ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets record by ID including soft-deleted ones
    /// </summary>
    public virtual async Task<T?> GetByIdWithDeletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken);
    }

    /// <summary>
    /// Finds records matching condition including soft-deleted ones
    /// </summary>
    public virtual async Task<IEnumerable<T>> FindWithDeletedAsync(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.IgnoreQueryFilters().Where(predicate).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Returns queryable ignoring soft delete filter
    /// </summary>
    public virtual IQueryable<T> GetQueryableWithDeleted()
    {
        return _dbSet.IgnoreQueryFilters();
    }
}
