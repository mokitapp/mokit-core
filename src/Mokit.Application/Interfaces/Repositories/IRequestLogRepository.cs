using Mokit.Domain.Entities;

namespace Mokit.Application.Interfaces.Repositories;

/// <summary>
/// RequestLog repository interface
/// </summary>
public interface IRequestLogRepository : Persistence.IRepository<RequestLog>
{
    Task<IEnumerable<RequestLog>> GetByProjectIdAsync(Guid projectId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<int> GetCountByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RequestLog>> GetByEndpointIdAsync(Guid endpointId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<int> ClearOldLogsAsync(DateTime before, CancellationToken cancellationToken = default);
}
