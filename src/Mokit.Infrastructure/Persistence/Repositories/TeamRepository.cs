using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mokit.Application.Interfaces.Repositories;
using Mokit.Domain.Entities;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Persistence.Repositories;

/// <summary>
/// Team repository implementation
/// </summary>
public class TeamRepository : Repository<Team>, ITeamRepository
{
    public TeamRepository(MokitDbContext context, ILogger<TeamRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<Team?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Team>> GetByUserMembershipAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Members)
            .Where(t => t.Members.Any(m => m.UserId == userId))
            .ToListAsync(cancellationToken);
    }
}
