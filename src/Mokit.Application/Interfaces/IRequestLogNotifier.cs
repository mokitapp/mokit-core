namespace Mokit.Application.Interfaces;

public interface IRequestLogNotifier
{
    Task NotifyRequestReceivedAsync(RequestLogNotification notification);
}

public class RequestLogNotification
{
    public Guid ProjectId { get; set; }
    public Guid? EndpointId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? QueryString { get; set; }
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    public bool IsMatched { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ClientIp { get; set; }
    public DateTime Timestamp { get; set; }
}

