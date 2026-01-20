using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mokit.Application.Interfaces.Repositories;
using Mokit.Domain.Entities;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Persistence.Repositories;

/// <summary>
/// MockResponse repository implementation
/// </summary>
public class MockResponseRepository : Repository<MockResponse>, IMockResponseRepository
{
    public MockResponseRepository(MokitDbContext context, ILogger<MockResponseRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<MockResponse>> GetByEndpointIdAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(r => r.EndpointId == endpointId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MockResponse>> GetOrderedByEndpointIdAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.EndpointId == endpointId)
            .OrderBy(r => r.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task<MockResponse?> GetDefaultResponseAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.EndpointId == endpointId && r.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
