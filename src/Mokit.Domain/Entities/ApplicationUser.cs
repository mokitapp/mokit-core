using Microsoft.AspNetCore.Identity;

namespace Mokit.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? CreatedByUserId { get; set; }
    
    // UI Preferences
    public string ThemePreference { get; set; } = "dark"; // "dark" or "light"
    public bool SidebarCollapsed { get; set; } = false;
    
    public string FullName => $"{FirstName} {LastName}".Trim();

    // Navigation properties
    public virtual ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
    public virtual ICollection<MockProject> PersonalProjects { get; set; } = new List<MockProject>();
}


