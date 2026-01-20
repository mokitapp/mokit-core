using Mokit.Domain.Common;
using Mokit.Domain.Enums;

namespace Mokit.Domain.Entities;

public class ValidationRule : BaseEntity
{
    public Guid EndpointId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public ParameterLocation Location { get; set; } = ParameterLocation.Query;
    public bool IsRequired { get; set; } = false;
    public string? DataType { get; set; }
    public string? RegexPattern { get; set; }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    public string? AllowedValues { get; set; } // Comma separated or JSON array
    public string? DefaultValue { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; } = 400; // Per-rule status code
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual MockEndpoint Endpoint { get; set; } = null!;
}


