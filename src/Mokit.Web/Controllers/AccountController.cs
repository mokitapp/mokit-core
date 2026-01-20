using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Mokit.Application.DTOs.User;
using Mokit.Application.Interfaces;
using Mokit.Domain.Entities;

namespace Mokit.Web.Controllers;

[Route("api/account")]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserService _userService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IUserService userService,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("setup-required")]
    public async Task<IActionResult> CheckSetupRequired()
    {
        var isRequired = await _userService.IsSetupRequiredAsync();
        return Ok(new { setupRequired = isRequired });
    }

    [HttpPost("setup")]
    public async Task<IActionResult> Setup([FromForm] SetupRequest request)
    {
        // Check if setup is still required
        if (!await _userService.IsSetupRequiredAsync())
        {
            return Redirect($"/Account/Login?Error={Uri.EscapeDataString("System is already set up.")}");
        }

        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return Redirect($"/Setup?Error={Uri.EscapeDataString("All fields are required.")}");
        }

        if (request.Password != request.ConfirmPassword)
        {
            return Redirect($"/Setup?Error={Uri.EscapeDataString("Passwords do not match.")}");
        }

        var dto = new SetupAdminDto
        {
            Email = request.Email,
            Password = request.Password,
            ConfirmPassword = request.ConfirmPassword,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await _userService.SetupAdminAsync(dto);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Admin user created: {Email}", request.Email);
            
            // Auto sign in after setup
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
            }
            
            return Redirect("/");
        }

        return Redirect($"/Setup?Error={Uri.EscapeDataString(result.Error ?? "An error occurred.")}");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] LoginRequest request)
    {
        // Redirect to setup if no admin exists
        if (await _userService.IsSetupRequiredAsync())
        {
            return Redirect("/Setup");
        }

        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return Redirect($"/Account/Login?Error={Uri.EscapeDataString("Email and password are required.")}");
        }

        // Check if user is active
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user != null && !user.IsActive)
        {
            return Redirect($"/Account/Login?Error={Uri.EscapeDataString("Your account has been disabled. Please contact an administrator.")}");
        }

        var result = await _signInManager.PasswordSignInAsync(
            request.Email, 
            request.Password, 
            request.RememberMe, 
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            // Update last login
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }
            
            _logger.LogInformation("User logged in: {Email}", request.Email);
            return Redirect(request.ReturnUrl ?? "/");
        }

        if (result.IsLockedOut)
        {
            return Redirect($"/Account/Login?Error={Uri.EscapeDataString("Your account has been temporarily locked.")}");
        }

        return Redirect($"/Account/Login?Error={Uri.EscapeDataString("Invalid email or password.")}");
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return Redirect("/Account/Login");
    }

    [Authorize]
    [HttpPost("create-user")]
    public async Task<IActionResult> CreateUser([FromForm] CreateUserRequest request)
    {
        // Get current user
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null || !currentUser.IsAdmin)
        {
            return Forbid();
        }

        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return Redirect($"/Admin/Users/New?Error={Uri.EscapeDataString("Email and password are required.")}");
        }

        var dto = new CreateUserDto
        {
            Email = request.Email,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsAdmin = request.IsAdmin
        };

        var result = await _userService.CreateUserAsync(currentUser.Id, dto);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Admin {AdminEmail} created new user: {Email}", currentUser.Email, request.Email);
            return Redirect($"/Admin/Users?Success={Uri.EscapeDataString("User created successfully.")}");
        }

        return Redirect($"/Admin/Users/New?Error={Uri.EscapeDataString(result.Error ?? "An error occurred.")}");
    }

    [Authorize]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromForm] ResetPasswordRequest request)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null || !currentUser.IsAdmin)
        {
            return Forbid();
        }

        var dto = new ResetPasswordDto
        {
            UserId = request.UserId,
            NewPassword = request.NewPassword
        };

        var result = await _userService.ResetPasswordAsync(dto);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Admin {AdminEmail} reset password for user: {UserId}", currentUser.Email, request.UserId);
            return Redirect($"/Admin/Users/{request.UserId}?Success={Uri.EscapeDataString("Password reset successfully.")}");
        }

        return Redirect($"/Admin/Users/{request.UserId}?Error={Uri.EscapeDataString(result.Error ?? "An error occurred.")}");
    }

    [Authorize]
    [HttpPost("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] PreferencesRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        if (request.Theme != null)
        {
            user.ThemePreference = request.Theme;
        }
        
        if (request.SidebarCollapsed.HasValue)
        {
            user.SidebarCollapsed = request.SidebarCollapsed.Value;
        }

        var result = await _userManager.UpdateAsync(user);
        
        if (result.Succeeded)
        {
            return Ok(new { success = true });
        }

        return BadRequest(new { success = false, error = "Preferences could not be updated." });
    }

    [Authorize]
    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(new 
        { 
            theme = user.ThemePreference ?? "dark",
            sidebarCollapsed = user.SidebarCollapsed
        });
    }
}

public class SetupRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class CreateUserRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsAdmin { get; set; }
}

public class ResetPasswordRequest
{
    public string UserId { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public class PreferencesRequest
{
    public string? Theme { get; set; }
    public bool? SidebarCollapsed { get; set; }
}
