using Mokit.Domain.Entities;

namespace Mokit.Application.Interfaces.Repositories;

/// <summary>
/// Team repository interface
/// </summary>
public interface ITeamRepository : Persistence.IRepository<Team>
{
    Task<Team?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Team>> GetByUserMembershipAsync(string userId, CancellationToken cancellationToken = default);
}
