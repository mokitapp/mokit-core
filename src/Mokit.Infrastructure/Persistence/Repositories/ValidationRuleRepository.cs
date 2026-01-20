using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mokit.Application.Interfaces.Repositories;
using Mokit.Domain.Entities;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Persistence.Repositories;

public class ValidationRuleRepository : Repository<ValidationRule>, IValidationRuleRepository
{
    public ValidationRuleRepository(MokitDbContext context, ILogger<ValidationRuleRepository> logger)
        : base(context, logger) { }

    public async Task<IEnumerable<ValidationRule>> GetByEndpointIdAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.EndpointId == endpointId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ValidationRule>> GetActiveByEndpointIdAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.EndpointId == endpointId && r.IsActive)
            .ToListAsync(cancellationToken);
    }
}
