using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mokit.Application.Interfaces.Repositories;
using Mokit.Domain.Entities;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Persistence.Repositories;

public class TeamMemberRepository : Repository<TeamMember>, ITeamMemberRepository
{
    public TeamMemberRepository(MokitDbContext context, ILogger<TeamMemberRepository> logger)
        : base(context, logger) { }

    public async Task<IEnumerable<TeamMember>> GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.TeamId == teamId && m.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TeamMember>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.UserId == userId && m.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<TeamMember?> GetByTeamAndUserAsync(Guid teamId, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId, cancellationToken);
    }

    public async Task<bool> IsUserMemberAsync(Guid teamId, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(m => m.TeamId == teamId && m.UserId == userId && m.IsActive, cancellationToken);
    }
}
