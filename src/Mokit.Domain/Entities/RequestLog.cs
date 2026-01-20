using Mokit.Domain.Common;

namespace Mokit.Domain.Entities;

public class RequestLog : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid? EndpointId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? QueryString { get; set; }
    public string? RequestHeaders { get; set; }
    public string? RequestBody { get; set; }
    public int ResponseStatusCode { get; set; }
    public string? ResponseHeaders { get; set; }
    public string? ResponseBody { get; set; }
    public long DurationMs { get; set; }
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
    public bool IsMatched { get; set; } = true;
    public string? MatchedRoute { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation properties
    public virtual MockProject Project { get; set; } = null!;
    public virtual MockEndpoint? Endpoint { get; set; }
}


