using Mokit.Domain.Common;
using Mokit.Domain.Enums;

namespace Mokit.Domain.Entities;

public class MockProject : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int Port { get; set; } = 0; // 0 means no dedicated port (hosted on main app subpath)
    
    // Ownership - either Team or User (personal)
    public Guid? TeamId { get; set; }
    public string? UserId { get; set; }
    
    // Global settings
    public int DefaultDelay { get; set; } = 0;
    public bool EnableCors { get; set; } = true;
    public bool EnableLogging { get; set; } = true;
    public bool EnableLatencySimulation { get; set; } = false;
    public int? GlobalLatencyMin { get; set; }
    public int? GlobalLatencyMax { get; set; }
    
    // JWT Settings
    public bool EnableJwtValidation { get; set; } = false;
    public string? JwtSecret { get; set; }
    public string? JwtIssuer { get; set; }
    public string? JwtAudience { get; set; }

    // Navigation properties
    public virtual Team? Team { get; set; }
    public virtual ICollection<MockEndpoint> Endpoints { get; set; } = new List<MockEndpoint>();
    public virtual ICollection<RequestLog> RequestLogs { get; set; } = new List<RequestLog>();
    public virtual ICollection<DynamicVariable> DynamicVariables { get; set; } = new List<DynamicVariable>();
}


