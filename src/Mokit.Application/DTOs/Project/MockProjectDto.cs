namespace Mokit.Application.DTOs.Project;

public class MockProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public Guid? TeamId { get; set; }
    public string? TeamName { get; set; }
    public string? TeamSlug { get; set; }
    public string? UserId { get; set; }
    public string? OwnerName { get; set; }
    public int EndpointCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Settings
    public int DefaultDelay { get; set; }
    public bool EnableCors { get; set; }
    public bool EnableLogging { get; set; }
    public bool EnableJwtValidation { get; set; }
    
    // Computed URL path
    public string MockUrl => TeamSlug != null 
        ? $"/{TeamSlug}/{Slug}" 
        : $"/{Slug}";
}

public class CreateMockProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; } // If null, auto-generated from Name
    public string? Description { get; set; }
    public Guid? TeamId { get; set; }
    public int DefaultDelay { get; set; } = 0;
    public bool EnableCors { get; set; } = true;
    public bool EnableLogging { get; set; } = true;
}

public class UpdateMockProjectDto
{
    public string Name { get; set; } = string.Empty;
    // Slug artık değiştirilemez - bu alan kullanılmıyor
    [Obsolete("Slug değiştirilemez")]
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DefaultDelay { get; set; } = 0;
    public bool EnableCors { get; set; } = true;
    public bool EnableLogging { get; set; } = true;
    public bool EnableLatencySimulation { get; set; } = false;
    public int? GlobalLatencyMin { get; set; }
    public int? GlobalLatencyMax { get; set; }
    public bool EnableJwtValidation { get; set; } = false;
    public string? JwtSecret { get; set; }
    public string? JwtIssuer { get; set; }
    public string? JwtAudience { get; set; }
}


