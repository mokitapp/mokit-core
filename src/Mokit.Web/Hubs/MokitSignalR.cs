using Microsoft.AspNetCore.SignalR;

namespace Mokit.Web.Hubs;

public class MokitSignalR : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public async Task JoinProjectGroup(string projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"project-{projectId}");
    }

    public async Task LeaveProjectGroup(string projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project-{projectId}");
    }
}

public class RequestLogDto
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

public interface IMokitClient
{
    Task RequestReceived(RequestLogDto log);
}
