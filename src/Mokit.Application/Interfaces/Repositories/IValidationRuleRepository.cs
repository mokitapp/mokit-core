using Mokit.Domain.Entities;

namespace Mokit.Application.Interfaces.Repositories;

/// <summary>
/// ValidationRule repository interface
/// </summary>
public interface IValidationRuleRepository : Persistence.IRepository<ValidationRule>
{
    Task<IEnumerable<ValidationRule>> GetByEndpointIdAsync(Guid endpointId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ValidationRule>> GetActiveByEndpointIdAsync(Guid endpointId, CancellationToken cancellationToken = default);
}
