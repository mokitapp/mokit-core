using Mokit.Domain.Entities;

namespace Mokit.Application.Interfaces.Repositories;

/// <summary>
/// TeamMember repository interface
/// </summary>
public interface ITeamMemberRepository : Persistence.IRepository<TeamMember>
{
    Task<IEnumerable<TeamMember>> GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TeamMember>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<TeamMember?> GetByTeamAndUserAsync(Guid teamId, string userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserMemberAsync(Guid teamId, string userId, CancellationToken cancellationToken = default);
}
