using Mokit.Domain.Common;

namespace Mokit.Domain.Entities;

public class MockResponse : BaseAuditableEntity
{
    public Guid EndpointId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StatusCode { get; set; } = 200;
    public string? Body { get; set; }
    public string ContentType { get; set; } = "application/json";
    public bool IsDefault { get; set; } = false;
    public int Order { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    
    // Headers stored as JSON
    public string? Headers { get; set; }
    
    // Conditional response
    public string? Condition { get; set; }
    public string? ConditionExpression { get; set; }
    
    // File response
    public bool IsFileResponse { get; set; } = false;
    public string? FilePath { get; set; }
    public string? FileName { get; set; }

    // Navigation properties
    public virtual MockEndpoint Endpoint { get; set; } = null!;
}


