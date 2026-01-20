using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mokit.Application.Common;
using Mokit.Application.DTOs.User;
using Mokit.Application.Interfaces;
using Mokit.Application.Interfaces.Persistence;
using Mokit.Domain.Entities;
using Mokit.Domain.Enums;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork<MokitDbContext> _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(
        IUnitOfWork<MokitDbContext> unitOfWork,
        UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task<bool> IsSetupRequiredAsync()
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        return !await scope.Context.Users.AnyAsync(u => u.IsAdmin);
    }

    public async Task<Result<UserDto>> SetupAdminAsync(SetupAdminDto dto)
    {
        if (!await IsSetupRequiredAsync())
        {
            return Result<UserDto>.Failure("System is already set up. Admin user exists.");
        }

        if (dto.Password != dto.ConfirmPassword)
        {
            return Result<UserDto>.Failure("Passwords do not match.");
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IsAdmin = true,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<UserDto>.Failure(errors);
        }

        return await GetByIdAsync(user.Id);
    }

    public async Task<Result<List<UserDto>>> GetAllUsersAsync()
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        var users = await scope.Context.Users
            .Include(u => u.TeamMemberships)
                .ThenInclude(tm => tm.Team)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var userDtos = users.Select(MapToDto).ToList();
        return Result<List<UserDto>>.Success(userDtos);
    }

    public async Task<Result<UserDto>> GetByIdAsync(string userId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        var user = await scope.Context.Users
            .Include(u => u.TeamMemberships)
                .ThenInclude(tm => tm.Team)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return Result<UserDto>.Failure("User not found.");
        }

        return Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result<UserDto>> CreateUserAsync(string adminUserId, CreateUserDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            return Result<UserDto>.Failure("This email address is already in use.");
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IsAdmin = dto.IsAdmin,
            IsActive = true,
            EmailConfirmed = true,
            CreatedByUserId = adminUserId
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<UserDto>.Failure(errors);
        }

        return await GetByIdAsync(user.Id);
    }

    public async Task<Result<UserDto>> UpdateUserAsync(string userId, UpdateUserDto dto)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var user = await scope.Context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "User not found.");
            }

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.AvatarUrl = dto.AvatarUrl;
            user.IsActive = dto.IsActive;
            user.IsAdmin = dto.IsAdmin;
            
            return (true, (string?)null);
        });

        if (!result.Item1)
        {
            return Result<UserDto>.Failure(result.Item2 ?? "Update failed");
        }

        return await GetByIdAsync(userId);
    }

    public async Task<Result> DeactivateUserAsync(string userId)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var user = await scope.Context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "User not found.");
            }

            if (user.IsAdmin)
            {
                var adminCount = await scope.Context.Users.CountAsync(u => u.IsAdmin && u.IsActive);
                if (adminCount <= 1)
                {
                    return (false, "The last administrator cannot be deactivated.");
                }
            }

            user.IsActive = false;
            return (true, (string?)null);
        });

        if (!result.Item1)
        {
            return Result.Failure(result.Item2 ?? "Deactivate failed");
        }

        return Result.Success();
    }

    public async Task<Result> ReactivateUserAsync(string userId)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var user = await scope.Context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "User not found.");
            }

            user.IsActive = true;
            return (true, (string?)null);
        });

        if (!result.Item1)
        {
            return Result.Failure(result.Item2 ?? "Reactivate failed");
        }

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null)
        {
            return Result.Failure("User not found.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(string userId, ChangePasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
        {
            return Result.Failure("New passwords do not match.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure("User not found.");
        }

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        return Result.Success();
    }

    public async Task<bool> IsAdminAsync(string userId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        var user = await scope.Context.Users.FindAsync(userId);
        return user?.IsAdmin ?? false;
    }

    public async Task<Result> AddUserToTeamAsync(string userId, Guid teamId, string role)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var user = await scope.Context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "User not found.");
            }

            var team = await scope.Context.Teams.FindAsync(teamId);
            if (team == null)
            {
                return (false, "Team not found.");
            }

            var existingMember = await scope.Context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TeamId == teamId);

            if (existingMember != null && existingMember.IsActive)
            {
                return (false, "User is already a member of this team.");
            }

            if (existingMember != null)
            {
                existingMember.IsActive = true;
                existingMember.Role = Enum.Parse<TeamRole>(role);
                existingMember.JoinedAt = DateTime.UtcNow;
            }
            else
            {
                var teamMember = new TeamMember
                {
                    TeamId = teamId,
                    UserId = userId,
                    Role = Enum.Parse<TeamRole>(role),
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };
                scope.Context.TeamMembers.Add(teamMember);
            }

            return (true, (string?)null);
        });

        if (!result.Item1)
        {
            return Result.Failure(result.Item2 ?? "Add to team failed");
        }

        return Result.Success();
    }

    public async Task<Result> RemoveUserFromTeamAsync(string userId, Guid teamId)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var membership = await scope.Context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TeamId == teamId);

            if (membership == null)
            {
                return (false, "Membership not found.");
            }

            membership.IsActive = false;
            return (true, (string?)null);
        });

        if (!result.Item1)
        {
            return Result.Failure(result.Item2 ?? "Remove from team failed");
        }

        return Result.Success();
    }

    private static UserDto MapToDto(ApplicationUser user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            IsActive = user.IsActive,
            IsAdmin = user.IsAdmin,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Teams = user.TeamMemberships
                .Where(tm => tm.IsActive)
                .Select(tm => new UserTeamDto
                {
                    TeamId = tm.TeamId,
                    TeamName = tm.Team?.Name ?? string.Empty,
                    TeamSlug = tm.Team?.Slug ?? string.Empty,
                    Role = tm.Role.ToString(),
                    JoinedAt = tm.JoinedAt ?? DateTime.UtcNow
                }).ToList()
        };
    }
}
