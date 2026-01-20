using Mokit.Domain.Enums;

namespace Mokit.Application.DTOs.Endpoint;

public class WebhookDefinitionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public HttpMethodType Method { get; set; }
    public string? Body { get; set; }
    public string? Headers { get; set; }
    public int DelayMs { get; set; }
    public bool IsEnabled { get; set; }
}
