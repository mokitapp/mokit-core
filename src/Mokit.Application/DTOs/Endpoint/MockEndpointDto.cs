using Mokit.Domain.Enums;

namespace Mokit.Application.DTOs.Endpoint;

public class MockEndpointDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Route { get; set; } = string.Empty;
    public HttpMethodType Method { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public bool IsWildcard { get; set; }
    public string? RegexPattern { get; set; }
    public ResponseSelectionMode ResponseMode { get; set; }
    public int? DelayMin { get; set; }
    public int? DelayMax { get; set; }
    public int ResponseCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ValidationErrorResponseTemplate { get; set; }
    public List<MockResponseDto>? Responses { get; set; }
    public List<ValidationRuleDto> ValidationRules { get; set; } = new();
    public List<WebhookDefinitionDto> Webhooks { get; set; } = new();
}

public class MockResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? Body { get; set; }
    public string ContentType { get; set; } = "application/json";
    public string? Headers { get; set; }
    public bool IsDefault { get; set; }
}

public class CreateMockEndpointDto
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Route { get; set; } = string.Empty;
    public HttpMethodType Method { get; set; } = HttpMethodType.GET;
    public bool IsWildcard { get; set; } = false;
    public string? RegexPattern { get; set; }
    public ResponseSelectionMode ResponseMode { get; set; } = ResponseSelectionMode.Sequential;
    public int? DelayMin { get; set; }
    public int? DelayMax { get; set; }
    public List<ValidationRuleDto> ValidationRules { get; set; } = new();
    public List<WebhookDefinitionDto> Webhooks { get; set; } = new();
}

public class UpdateMockEndpointDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Route { get; set; } = string.Empty;
    public HttpMethodType Method { get; set; }
    public bool IsActive { get; set; } = true;
    public int Order { get; set; }
    public bool IsWildcard { get; set; }
    public string? RegexPattern { get; set; }
    public ResponseSelectionMode ResponseMode { get; set; }
    public int? DelayMin { get; set; }
    public int? DelayMax { get; set; }
    // Webhooks are usually managed via separate endpoints, but basic update can be here if needed.
    // For now we will rely on Add/UpdateWebhook endpoints.
    public string? ValidationErrorResponseTemplate { get; set; }
    public List<ValidationRuleDto> ValidationRules { get; set; } = new();
    public List<WebhookDefinitionDto> Webhooks { get; set; } = new();
}

public class ValidationRuleDto
{
    public Guid Id { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public string Location { get; set; } = "Query"; // Query, Path, Header, Body
    public string DataType { get; set; } = "String"; // String, Number, Boolean, Email, Uuid, Date, Url, Array, Object
    public bool IsRequired { get; set; }
    public string? RegexPattern { get; set; }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    public string? AllowedValues { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; } = 400; // Per-rule status code (400, 401, 403, 415, 422, etc.)
}


