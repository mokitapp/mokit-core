using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mokit.Application.Interfaces.Repositories;
using Mokit.Domain.Entities;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Persistence.Repositories;

/// <summary>
/// RequestLog repository implementation
/// </summary>
public class RequestLogRepository : Repository<RequestLog>, IRequestLogRepository
{
    public RequestLogRepository(MokitDbContext context, ILogger<RequestLogRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<RequestLog>> GetByProjectIdAsync(Guid projectId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(r => r.ProjectId == projectId, cancellationToken);
    }

    public async Task<IEnumerable<RequestLog>> GetByEndpointIdAsync(Guid endpointId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.EndpointId == endpointId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ClearOldLogsAsync(DateTime before, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.CreatedAt < before)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
