using Mokit.Domain.Entities;

namespace Mokit.Application.Interfaces.Repositories;

/// <summary>
/// WebhookDefinition repository interface
/// </summary>
public interface IWebhookDefinitionRepository : Persistence.IRepository<WebhookDefinition>
{
    Task<IEnumerable<WebhookDefinition>> GetByEndpointIdAsync(Guid endpointId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WebhookDefinition>> GetEnabledByEndpointIdAsync(Guid endpointId, CancellationToken cancellationToken = default);
}
