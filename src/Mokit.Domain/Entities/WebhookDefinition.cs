using Mokit.Domain.Common;
using Mokit.Domain.Enums;

namespace Mokit.Domain.Entities;

public class WebhookDefinition : BaseAuditableEntity
{
    public Guid EndpointId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public HttpMethodType Method { get; set; } = HttpMethodType.POST;
    public string? Body { get; set; } // Supports templates
    public string? Headers { get; set; } // JSON dictionary, supports templates
    public int DelayMs { get; set; } = 0;
    public bool IsEnabled { get; set; } = true;
    
    // Navigation
    public virtual MockEndpoint Endpoint { get; set; } = null!;
}
