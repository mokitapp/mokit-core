using Microsoft.AspNetCore.SignalR;
using Mokit.Application.Interfaces;
using Mokit.Web.Hubs;

namespace Mokit.Web.Services;

public class SignalRRequestLogNotifier : IRequestLogNotifier
{
    private readonly IHubContext<MokitSignalR> _hubContext;

    public SignalRRequestLogNotifier(IHubContext<MokitSignalR> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyRequestReceivedAsync(RequestLogNotification notification)
    {
        var dto = new RequestLogDto
        {
            ProjectId = notification.ProjectId,
            EndpointId = notification.EndpointId,
            Method = notification.Method,
            Path = notification.Path,
            QueryString = notification.QueryString,
            StatusCode = notification.StatusCode,
            DurationMs = notification.DurationMs,
            IsMatched = notification.IsMatched,
            ErrorMessage = notification.ErrorMessage,
            ClientIp = notification.ClientIp,
            Timestamp = notification.Timestamp
        };

        // Send to all connected clients
        await _hubContext.Clients.All.SendAsync("RequestReceived", dto);
    }
}

