using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mokit.Application.Interfaces.Repositories;
using Mokit.Domain.Entities;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Persistence.Repositories;

public class WebhookDefinitionRepository : Repository<WebhookDefinition>, IWebhookDefinitionRepository
{
    public WebhookDefinitionRepository(MokitDbContext context, ILogger<WebhookDefinitionRepository> logger)
        : base(context, logger) { }

    public async Task<IEnumerable<WebhookDefinition>> GetByEndpointIdAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(w => w.EndpointId == endpointId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WebhookDefinition>> GetEnabledByEndpointIdAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(w => w.EndpointId == endpointId && w.IsEnabled)
            .ToListAsync(cancellationToken);
    }
}
