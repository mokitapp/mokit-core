namespace Mokit.Application.DTOs.Response;

public class MockResponseDto
{
    public Guid Id { get; set; }
    public Guid EndpointId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StatusCode { get; set; }
    public string? Body { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public string? Condition { get; set; }
    public string? ConditionExpression { get; set; }
    public bool IsFileResponse { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateMockResponseDto
{
    public Guid EndpointId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StatusCode { get; set; } = 200;
    public string? Body { get; set; }
    public string ContentType { get; set; } = "application/json";
    public bool IsDefault { get; set; } = false;
    public Dictionary<string, string>? Headers { get; set; }
    public string? Condition { get; set; }
    public string? ConditionExpression { get; set; }
}

public class UpdateMockResponseDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StatusCode { get; set; }
    public string? Body { get; set; }
    public string ContentType { get; set; } = "application/json";
    public bool IsDefault { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, string>? Headers { get; set; }
    public string? Condition { get; set; }
    public string? ConditionExpression { get; set; }
    public bool IsFileResponse { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
}


