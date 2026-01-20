using Mokit.Application.Common;
using Mokit.Application.DTOs.Team;

namespace Mokit.Application.Interfaces;

public interface ITeamService
{
    Task<Result<List<TeamDto>>> GetAllTeamsAsync();
    Task<Result<List<TeamDto>>> GetUserTeamsAsync(string userId);
    Task<Result<TeamDto>> GetByIdAsync(Guid teamId);
    Task<Result<TeamDto>> GetBySlugAsync(string slug);
    Task<Result<TeamDto>> CreateAsync(string userId, CreateTeamDto dto);
    Task<Result<TeamDto>> UpdateAsync(Guid teamId, UpdateTeamDto dto);
    Task<Result> DeleteAsync(Guid teamId);
    Task<Result<List<TeamMemberDto>>> GetMembersAsync(Guid teamId);
    Task<Result<TeamMemberDto>> AddMemberAsync(Guid teamId, AddTeamMemberDto dto);
    Task<Result> RemoveMemberAsync(Guid teamId, string userId);
    Task<Result> UpdateMemberRoleAsync(Guid teamId, string userId, string role);
    Task<bool> IsUserTeamMemberAsync(Guid teamId, string userId);
    Task<bool> IsUserTeamAdminAsync(Guid teamId, string userId);
    Task<bool> IsSlugAvailableAsync(string slug, Guid? excludeTeamId = null);
}


