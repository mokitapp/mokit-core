using Mokit.Domain.Entities;

namespace Mokit.Application.Interfaces.Repositories;

/// <summary>
/// MockResponse repository interface
/// </summary>
public interface IMockResponseRepository : Persistence.IRepository<MockResponse>
{
    Task<IEnumerable<MockResponse>> GetByEndpointIdAsync(Guid endpointId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MockResponse>> GetOrderedByEndpointIdAsync(Guid endpointId, CancellationToken cancellationToken = default);
    Task<MockResponse?> GetDefaultResponseAsync(Guid endpointId, CancellationToken cancellationToken = default);
}
