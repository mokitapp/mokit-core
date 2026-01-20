using Mokit.Domain.Enums;

namespace Mokit.HostManager.Management;

public class MockServerInfo
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int Port { get; set; }
    public MockServerStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public long RequestCount { get; set; }
    public long ErrorCount { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastRequestAt { get; set; }
}


