using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mokit.Application.Common;
using Mokit.Application.DTOs.Team;
using Mokit.Application.Helpers;
using Mokit.Application.Interfaces;
using Mokit.Application.Interfaces.Persistence;
using Mokit.Domain.Entities;
using Mokit.Domain.Enums;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Services;

public class TeamService : ITeamService
{
    private readonly IUnitOfWork<MokitDbContext> _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;

    public TeamService(
        IUnitOfWork<MokitDbContext> unitOfWork,
        UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task<Result<List<TeamDto>>> GetUserTeamsAsync(string userId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        var teams = await scope.Context.Teams
            .Include(t => t.Members)
            .Include(t => t.Projects)
                .ThenInclude(p => p.Endpoints)
            .Where(t => t.Members.Any(m => m.UserId == userId && m.IsActive))
            .ToListAsync();

        var teamDtos = teams.Select(t => new TeamDto
        {
            Id = t.Id,
            Name = t.Name,
            Slug = t.Slug,
            Description = t.Description,
            LogoUrl = t.LogoUrl,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt,
            MemberCount = t.Members.Count(m => m.IsActive),
            ProjectCount = t.Projects.Count(p => p.IsActive),
            Projects = t.Projects.Where(p => p.IsActive).Select(p => new TeamProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Description = p.Description,
                IsActive = p.IsActive,
                EndpointCount = p.Endpoints.Count,
                MockUrl = $"/{t.Slug}/{p.Slug}"
            }).ToList()
        }).ToList();

        return Result<List<TeamDto>>.Success(teamDtos);
    }

    public async Task<Result<List<TeamDto>>> GetAllTeamsAsync()
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        var teams = await scope.Context.Teams
            .Include(t => t.Members)
            .Include(t => t.Projects)
                .ThenInclude(p => p.Endpoints)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var teamDtos = teams.Select(t => new TeamDto
        {
            Id = t.Id,
            Name = t.Name,
            Slug = t.Slug,
            Description = t.Description,
            LogoUrl = t.LogoUrl,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt,
            MemberCount = t.Members.Count(m => m.IsActive),
            ProjectCount = t.Projects.Count(p => p.IsActive),
            Projects = t.Projects.Where(p => p.IsActive).Select(p => new TeamProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Description = p.Description,
                IsActive = p.IsActive,
                EndpointCount = p.Endpoints.Count,
                MockUrl = $"/{t.Slug}/{p.Slug}"
            }).ToList()
        }).ToList();

        return Result<List<TeamDto>>.Success(teamDtos);
    }

    public async Task<Result<TeamDto>> GetByIdAsync(Guid teamId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        var team = await scope.Context.Teams
            .Include(t => t.Members)
            .Include(t => t.Projects)
                .ThenInclude(p => p.Endpoints)
            .FirstOrDefaultAsync(t => t.Id == teamId);

        if (team == null)
        {
            return Result<TeamDto>.Failure("Team not found");
        }

        return Result<TeamDto>.Success(MapToDto(team));
    }

    public async Task<Result<TeamDto>> GetBySlugAsync(string slug)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        var team = await scope.Context.Teams
            .Include(t => t.Members)
            .Include(t => t.Projects)
                .ThenInclude(p => p.Endpoints)
            .FirstOrDefaultAsync(t => t.Slug == slug);

        if (team == null)
        {
            return Result<TeamDto>.Failure("Team not found");
        }

        return Result<TeamDto>.Success(MapToDto(team));
    }

    public async Task<Result<TeamDto>> CreateAsync(string userId, CreateTeamDto dto)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            // Generate slug
            string slug;
            if (!string.IsNullOrEmpty(dto.Slug))
            {
                slug = SlugHelper.GenerateSlug(dto.Slug);
            }
            else
            {
                slug = SlugHelper.GenerateSlug(dto.Name);
            }

            // Ensure slug uniqueness
            slug = SlugHelper.GenerateUniqueSlug(slug, s => scope.Context.Teams.Any(t => t.Slug == s));

            var team = new Team
            {
                Name = dto.Name,
                Slug = slug,
                Description = dto.Description,
                LogoUrl = dto.LogoUrl,
                CreatedBy = userId
            };

            scope.Context.Teams.Add(team);

            // Add creator as owner
            var teamMember = new TeamMember
            {
                TeamId = team.Id,
                UserId = userId,
                Role = TeamRole.Owner,
                JoinedAt = DateTime.UtcNow
            };

            scope.Context.TeamMembers.Add(teamMember);

            return new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                Slug = team.Slug,
                Description = team.Description,
                LogoUrl = team.LogoUrl,
                IsActive = team.IsActive,
                CreatedAt = team.CreatedAt,
                MemberCount = 1,
                ProjectCount = 0,
                Projects = new List<TeamProjectDto>()
            };
        });

        return Result<TeamDto>.Success(result);
    }

    public async Task<Result<TeamDto>> UpdateAsync(Guid teamId, UpdateTeamDto dto)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var team = await scope.Context.Teams
                .Include(t => t.Members)
                .Include(t => t.Projects)
                    .ThenInclude(p => p.Endpoints)
                .FirstOrDefaultAsync(t => t.Id == teamId);

            if (team == null)
            {
                return (false, (TeamDto?)null, "Team not found");
            }

            // Regenerate slug if name changed
            if (team.Name != dto.Name)
            {
                var newSlug = SlugHelper.GenerateSlug(dto.Name);
                newSlug = SlugHelper.GenerateUniqueSlug(newSlug, s => 
                    scope.Context.Teams.Any(t => t.Id != teamId && t.Slug == s));
                team.Slug = newSlug;
            }

            team.Name = dto.Name;
            team.Description = dto.Description;
            team.LogoUrl = dto.LogoUrl;
            team.IsActive = dto.IsActive;
            team.UpdatedAt = DateTime.UtcNow;

            return (true, MapToDto(team), (string?)null);
        });

        if (!result.Item1)
        {
            return Result<TeamDto>.Failure(result.Item3 ?? "Update failed");
        }

        return Result<TeamDto>.Success(result.Item2!);
    }

    public async Task<Result> DeleteAsync(Guid teamId)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var team = await scope.Context.Teams.FindAsync(teamId);
            if (team == null)
            {
                return (false, "Team not found");
            }

            scope.Context.Teams.Remove(team);
            return (true, (string?)null);
        });

        if (!result.Item1)
        {
            return Result.Failure(result.Item2 ?? "Delete failed");
        }

        return Result.Success();
    }

    public async Task<Result<List<TeamMemberDto>>> GetMembersAsync(Guid teamId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        var members = await scope.Context.TeamMembers
            .Where(m => m.TeamId == teamId && m.IsActive)
            .ToListAsync();

        var memberDtos = new List<TeamMemberDto>();
        foreach (var member in members)
        {
            var user = await _userManager.FindByIdAsync(member.UserId);
            if (user != null)
            {
                memberDtos.Add(new TeamMemberDto
                {
                    Id = member.Id,
                    UserId = member.UserId,
                    UserEmail = user.Email ?? string.Empty,
                    UserName = user.FullName,
                    AvatarUrl = user.AvatarUrl,
                    Role = member.Role.ToString(),
                    JoinedAt = member.JoinedAt
                });
            }
        }

        return Result<List<TeamMemberDto>>.Success(memberDtos);
    }

    public async Task<Result<TeamMemberDto>> AddMemberAsync(Guid teamId, AddTeamMemberDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return Result<TeamMemberDto>.Failure("User not found");
        }

        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var existingMember = await scope.Context.TeamMembers
                .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == user.Id);

            if (existingMember != null)
            {
                if (existingMember.IsActive)
                {
                    return (false, (TeamMemberDto?)null, "User is already a member");
                }
                existingMember.IsActive = true;
                existingMember.Role = Enum.Parse<TeamRole>(dto.Role);
                existingMember.JoinedAt = DateTime.UtcNow;
            }
            else
            {
                var member = new TeamMember
                {
                    TeamId = teamId,
                    UserId = user.Id,
                    Role = Enum.Parse<TeamRole>(dto.Role),
                    JoinedAt = DateTime.UtcNow
                };
                scope.Context.TeamMembers.Add(member);
            }

            return (true, (TeamMemberDto?)null, (string?)null);
        });

        if (!result.Item1)
        {
            return Result<TeamMemberDto>.Failure(result.Item3 ?? "Add member failed");
        }

        // Get updated member info
        await using var readScope = await _unitOfWork.CreateScopeAsync();
        var updatedMember = await readScope.Context.TeamMembers
            .FirstAsync(m => m.TeamId == teamId && m.UserId == user.Id);

        return Result<TeamMemberDto>.Success(new TeamMemberDto
        {
            Id = updatedMember.Id,
            UserId = user.Id,
            UserEmail = user.Email ?? string.Empty,
            UserName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Role = updatedMember.Role.ToString(),
            JoinedAt = updatedMember.JoinedAt
        });
    }

    public async Task<Result> RemoveMemberAsync(Guid teamId, string userId)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var member = await scope.Context.TeamMembers
                .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId);

            if (member == null)
            {
                return (false, "Member not found");
            }

            if (member.Role == TeamRole.Owner)
            {
                return (false, "Cannot remove team owner");
            }

            member.IsActive = false;
            return (true, (string?)null);
        });

        if (!result.Item1)
        {
            return Result.Failure(result.Item2 ?? "Remove member failed");
        }

        return Result.Success();
    }

    public async Task<Result> UpdateMemberRoleAsync(Guid teamId, string userId, string role)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var member = await scope.Context.TeamMembers
                .FirstOrDefaultAsync(m => m.TeamId == teamId && m.UserId == userId);

            if (member == null)
            {
                return (false, "Member not found");
            }

            if (!Enum.TryParse<TeamRole>(role, out var newRole))
            {
                return (false, "Invalid role");
            }

            member.Role = newRole;
            return (true, (string?)null);
        });

        if (!result.Item1)
        {
            return Result.Failure(result.Item2 ?? "Update role failed");
        }

        return Result.Success();
    }

    public async Task<bool> IsUserTeamMemberAsync(Guid teamId, string userId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        return await scope.Context.TeamMembers
            .AnyAsync(m => m.TeamId == teamId && m.UserId == userId && m.IsActive);
    }

    public async Task<bool> IsUserTeamAdminAsync(Guid teamId, string userId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        return await scope.Context.TeamMembers
            .AnyAsync(m => m.TeamId == teamId && 
                          m.UserId == userId && 
                          m.IsActive && 
                          (m.Role == TeamRole.Owner || m.Role == TeamRole.Admin));
    }

    public async Task<bool> IsSlugAvailableAsync(string slug, Guid? excludeTeamId = null)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        if (excludeTeamId.HasValue)
        {
            return !await scope.Context.Teams.AnyAsync(t => t.Id != excludeTeamId && t.Slug == slug);
        }
        return !await scope.Context.Teams.AnyAsync(t => t.Slug == slug);
    }

    private TeamDto MapToDto(Team team)
    {
        return new TeamDto
        {
            Id = team.Id,
            Name = team.Name,
            Slug = team.Slug,
            Description = team.Description,
            LogoUrl = team.LogoUrl,
            IsActive = team.IsActive,
            CreatedAt = team.CreatedAt,
            MemberCount = team.Members.Count(m => m.IsActive),
            ProjectCount = team.Projects.Count(p => p.IsActive),
            Projects = team.Projects.Where(p => p.IsActive).Select(p => new TeamProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Description = p.Description,
                IsActive = p.IsActive,
                EndpointCount = p.Endpoints.Count,
                MockUrl = $"/{team.Slug}/{p.Slug}"
            }).ToList()
        };
    }
}
