using Mokit.Application.Common;
using Mokit.Application.DTOs.User;

namespace Mokit.Application.Interfaces;

public interface IUserService
{
    /// <summary>
    /// Checks if initial setup is required (no admin exists)
    /// </summary>
    Task<bool> IsSetupRequiredAsync();
    
    /// <summary>
    /// Creates the initial admin user during setup
    /// </summary>
    Task<Result<UserDto>> SetupAdminAsync(SetupAdminDto dto);
    
    /// <summary>
    /// Gets all users (admin only)
    /// </summary>
    Task<Result<List<UserDto>>> GetAllUsersAsync();
    
    /// <summary>
    /// Gets a user by ID
    /// </summary>
    Task<Result<UserDto>> GetByIdAsync(string userId);
    
    /// <summary>
    /// Creates a new user (admin only)
    /// </summary>
    Task<Result<UserDto>> CreateUserAsync(string adminUserId, CreateUserDto dto);
    
    /// <summary>
    /// Updates a user
    /// </summary>
    Task<Result<UserDto>> UpdateUserAsync(string userId, UpdateUserDto dto);
    
    /// <summary>
    /// Deactivates a user
    /// </summary>
    Task<Result> DeactivateUserAsync(string userId);
    
    /// <summary>
    /// Reactivates a user
    /// </summary>
    Task<Result> ReactivateUserAsync(string userId);
    
    /// <summary>
    /// Resets a user's password (admin only)
    /// </summary>
    Task<Result> ResetPasswordAsync(ResetPasswordDto dto);
    
    /// <summary>
    /// Changes the current user's password
    /// </summary>
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordDto dto);
    
    /// <summary>
    /// Checks if a user is admin
    /// </summary>
    Task<bool> IsAdminAsync(string userId);
    
    /// <summary>
    /// Adds a user to a team (admin only)
    /// </summary>
    Task<Result> AddUserToTeamAsync(string userId, Guid teamId, string role);
    
    /// <summary>
    /// Removes a user from a team (admin only)
    /// </summary>
    Task<Result> RemoveUserFromTeamAsync(string userId, Guid teamId);
}

