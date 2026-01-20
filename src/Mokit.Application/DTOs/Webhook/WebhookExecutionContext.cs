namespace Mokit.Application.DTOs.Webhook;

public class WebhookExecutionContext
{
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public Dictionary<string, string> QueryParams { get; set; } = new();
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, string> RouteParams { get; set; } = new();
    public object? Body { get; set; }
    public string? RawBody { get; set; }
}
