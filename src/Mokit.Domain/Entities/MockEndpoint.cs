using Mokit.Domain.Common;
using Mokit.Domain.Enums;

namespace Mokit.Domain.Entities;

public class MockEndpoint : BaseAuditableEntity
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Route { get; set; } = string.Empty;
    public HttpMethodType Method { get; set; } = HttpMethodType.GET;
    public bool IsActive { get; set; } = true;
    public int Order { get; set; } = 0;
    
    // Route settings
    public bool IsWildcard { get; set; } = false;
    public string? RegexPattern { get; set; }
    
    // Response settings
    public ResponseSelectionMode ResponseMode { get; set; } = ResponseSelectionMode.Sequential;
    public int CurrentResponseIndex { get; set; } = 0;
    
    // Delay settings (overrides project defaults)
    public int? DelayMin { get; set; }
    public int? DelayMax { get; set; }
    


    // Custom Validation Error Response Template
    public string? ValidationErrorResponseTemplate { get; set; }

    // Navigation properties
    public virtual MockProject Project { get; set; } = null!;
    public virtual ICollection<MockResponse> Responses { get; set; } = new List<MockResponse>();
    public virtual ICollection<ValidationRule> ValidationRules { get; set; } = new List<ValidationRule>();
    public virtual ICollection<WebhookDefinition> Webhooks { get; set; } = new List<WebhookDefinition>();
}

