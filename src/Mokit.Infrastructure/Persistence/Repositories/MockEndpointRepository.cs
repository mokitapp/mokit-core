using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mokit.Application.Interfaces.Repositories;
using Mokit.Domain.Entities;
using Mokit.Domain.Enums;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Persistence.Repositories;

/// <summary>
/// MockEndpoint repository implementation
/// </summary>
public class MockEndpointRepository : Repository<MockEndpoint>, IMockEndpointRepository
{
    public MockEndpointRepository(MokitDbContext context, ILogger<MockEndpointRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<MockEndpoint>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(e => e.ProjectId == projectId).ToListAsync(cancellationToken);
    }

    public async Task<MockEndpoint?> GetWithResponsesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.Responses)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<MockEndpoint?> GetByRouteAndMethodAsync(Guid projectId, string route, HttpMethodType method, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(e => 
            e.ProjectId == projectId && 
            e.Route == route && 
            e.Method == method, cancellationToken);
    }

    public async Task<IEnumerable<MockEndpoint>> GetActiveByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.ProjectId == projectId && e.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MockEndpoint>> GetOrderedByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.ProjectId == projectId)
            .OrderBy(e => e.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MockEndpoint>> GetWildcardEndpointsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.ProjectId == projectId && e.IsWildcard)
            .ToListAsync(cancellationToken);
    }
}
