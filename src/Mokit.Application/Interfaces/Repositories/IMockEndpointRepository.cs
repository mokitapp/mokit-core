using Mokit.Domain.Entities;
using Mokit.Domain.Enums;

namespace Mokit.Application.Interfaces.Repositories;

/// <summary>
/// MockEndpoint repository interface
/// </summary>
public interface IMockEndpointRepository : Persistence.IRepository<MockEndpoint>
{
    Task<IEnumerable<MockEndpoint>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<MockEndpoint?> GetWithResponsesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MockEndpoint?> GetByRouteAndMethodAsync(Guid projectId, string route, HttpMethodType method, CancellationToken cancellationToken = default);
    Task<IEnumerable<MockEndpoint>> GetActiveByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MockEndpoint>> GetOrderedByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MockEndpoint>> GetWildcardEndpointsAsync(Guid projectId, CancellationToken cancellationToken = default);
}
