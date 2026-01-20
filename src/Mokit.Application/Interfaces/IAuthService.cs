using Mokit.Application.Common;
using Mokit.Application.DTOs.Auth;
using ChangePasswordDto = Mokit.Application.DTOs.User.ChangePasswordDto;

namespace Mokit.Application.Interfaces;

public interface IAuthService
{
    Task<Result<UserDto>> RegisterAsync(RegisterDto dto);
    Task<Result<UserDto>> LoginAsync(LoginDto dto);
    Task<Result> LogoutAsync();
    Task<Result<UserDto>> GetCurrentUserAsync();
    Task<Result<UserDto>> UpdateProfileAsync(string userId, UpdateProfileDto dto);
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordDto dto);
}

public class UpdateProfileDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}


