using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Mokit.Domain.Entities;

namespace Mokit.Infrastructure.Identity;

/// <summary>
/// Custom claims principal factory that adds IsAdmin claim during authentication
/// </summary>
public class MokitClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public MokitClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        
        // Add custom claims
        identity.AddClaim(new Claim("IsAdmin", user.IsAdmin.ToString()));
        identity.AddClaim(new Claim("FullName", user.FullName ?? ""));
        
        return identity;
    }
}
