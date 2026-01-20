using Mokit.Domain.Entities;

namespace Mokit.HostManager.Management;

public interface IMockHostManager
{
    Task<bool> StartServerAsync(MockProject project);
    Task<bool> StopServerAsync(Guid projectId);
    Task<bool> RestartServerAsync(Guid projectId);
    MockServerInfo? GetServerStatus(Guid projectId);
    IEnumerable<MockServerInfo> GetAllServers();
    bool IsServerRunning(Guid projectId);
    Task UpdateEndpointsAsync(Guid projectId, IEnumerable<MockEndpoint> endpoints);
    event EventHandler<MockServerEventArgs>? ServerStatusChanged;
    event EventHandler<MockRequestEventArgs>? RequestReceived;
}

public class MockServerEventArgs : EventArgs
{
    public Guid ProjectId { get; set; }
    public MockServerInfo ServerInfo { get; set; } = null!;
}

public class MockRequestEventArgs : EventArgs
{
    public Guid ProjectId { get; set; }
    public Guid? EndpointId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    public bool IsMatched { get; set; }
    public DateTime Timestamp { get; set; }
}


