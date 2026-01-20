using Mokit.Domain.Entities;

namespace Mokit.Application.Interfaces.Repositories;

/// <summary>
/// MockProject repository interface
/// </summary>
public interface IMockProjectRepository : Persistence.IRepository<MockProject>
{
    Task<MockProject?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<MockProject>> GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MockProject>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<MockProject?> GetWithEndpointsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<MockProject>> GetActiveProjectsAsync(CancellationToken cancellationToken = default);
    Task<bool> IsSlugUniqueAsync(string slug, Guid? excludeProjectId = null, CancellationToken cancellationToken = default);
    Task<MockProject?> GetByPortAsync(int port, CancellationToken cancellationToken = default);
}
