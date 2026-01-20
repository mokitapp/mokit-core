using Microsoft.AspNetCore.Identity;
using Mokit.Application.Common;
using Mokit.Application.DTOs.Auth;
using ChangePasswordDto = Mokit.Application.DTOs.User.ChangePasswordDto;
using Mokit.Application.Interfaces;
using Mokit.Domain.Entities;

namespace Mokit.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<Result<UserDto>> RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            return Result<UserDto>.Failure("Email already registered");
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return Result<UserDto>.Failure(result.Errors.Select(e => e.Description).ToList());
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        return Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result<UserDto>> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return Result<UserDto>.Failure("Invalid email or password");
        }

        if (!user.IsActive)
        {
            return Result<UserDto>.Failure("Account is disabled");
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, dto.Password, dto.RememberMe, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                return Result<UserDto>.Failure("Account locked. Please try again later.");
            }
            return Result<UserDto>.Failure("Invalid email or password");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result> LogoutAsync()
    {
        await _signInManager.SignOutAsync();
        return Result.Success();
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync()
    {
        // This will be called with the current user context
        return await Task.FromResult(Result<UserDto>.Failure("Not authenticated"));
    }

    public async Task<Result<UserDto>> UpdateProfileAsync(string userId, UpdateProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result<UserDto>.Failure("User not found");
        }

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.AvatarUrl = dto.AvatarUrl;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return Result<UserDto>.Failure(result.Errors.Select(e => e.Description).ToList());
        }

        return Result<UserDto>.Success(MapToDto(user));
    }

    public async Task<Result> ChangePasswordAsync(string userId, ChangePasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Failure("User not found");
        }

        if (dto.NewPassword != dto.ConfirmPassword)
        {
            return Result.Failure("Passwords do not match");
        }

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            return Result.Failure(result.Errors.Select(e => e.Description).ToList());
        }

        return Result.Success();
    }

    private static UserDto MapToDto(ApplicationUser user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
