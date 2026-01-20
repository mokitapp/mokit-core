using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mokit.Application.Interfaces.Repositories;
using Mokit.Domain.Entities;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Persistence.Repositories;

/// <summary>
/// MockProject repository implementation
/// </summary>
public class MockProjectRepository : Repository<MockProject>, IMockProjectRepository
{
    public MockProjectRepository(MokitDbContext context, ILogger<MockProjectRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<MockProject?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
    }

    public async Task<IEnumerable<MockProject>> GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(p => p.TeamId == teamId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MockProject>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(p => p.UserId == userId && p.TeamId == null).ToListAsync(cancellationToken);
    }

    public async Task<MockProject?> GetWithEndpointsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.Endpoints)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<MockProject>> GetActiveProjectsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(p => p.IsActive).ToListAsync(cancellationToken);
    }

    public async Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeProjectId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(p => p.Slug == slug);
        if (excludeProjectId.HasValue)
            query = query.Where(p => p.Id != excludeProjectId.Value);
        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<MockProject?> GetByPortAsync(int port, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Port == port && p.Port > 0, cancellationToken);
    }
}
