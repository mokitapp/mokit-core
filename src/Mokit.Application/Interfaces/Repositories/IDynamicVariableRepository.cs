using Mokit.Domain.Entities;

namespace Mokit.Application.Interfaces.Repositories;

/// <summary>
/// DynamicVariable repository interface
/// </summary>
public interface IDynamicVariableRepository : Persistence.IRepository<DynamicVariable>
{
    Task<IEnumerable<DynamicVariable>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<DynamicVariable?> GetByProjectAndNameAsync(Guid projectId, string name, CancellationToken cancellationToken = default);
}
