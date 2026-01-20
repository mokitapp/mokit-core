using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mokit.Application.Interfaces.Repositories;
using Mokit.Domain.Entities;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Persistence.Repositories;

public class DynamicVariableRepository : Repository<DynamicVariable>, IDynamicVariableRepository
{
    public DynamicVariableRepository(MokitDbContext context, ILogger<DynamicVariableRepository> logger)
        : base(context, logger) { }

    public async Task<IEnumerable<DynamicVariable>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(v => v.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }

    public async Task<DynamicVariable?> GetByProjectAndNameAsync(Guid projectId, string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(v => v.ProjectId == projectId && v.Name == name, cancellationToken);
    }
}
