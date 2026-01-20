namespace Mokit.Application.DTOs.Team;

public class TeamDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public int ProjectCount { get; set; }
    public List<TeamProjectDto> Projects { get; set; } = new();
}

public class TeamProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int EndpointCount { get; set; }
    public string MockUrl { get; set; } = string.Empty;
}

public class CreateTeamDto
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; } // If null, auto-generated from Name
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
}

public class UpdateTeamDto
{
    public string Name { get; set; } = string.Empty;
    // Slug artık değiştirilemez - bu alan kullanılmıyor
    [Obsolete("Slug değiştirilemez")]
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TeamMemberDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime? JoinedAt { get; set; }
}

public class AddTeamMemberDto
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
}


