using Mokit.Application.Common;

namespace Mokit.Application.Interfaces;

public interface IRequestLogService
{
    /// <summary>
    /// Gets logs for admin (all logs)
    /// </summary>
    Task<Result<List<RequestLogDetailDto>>> GetAllLogsAsync(int page = 1, int pageSize = 100, Guid? projectId = null);
    
    /// <summary>
    /// Gets logs for a specific user (only their projects and team projects)
    /// </summary>
    Task<Result<List<RequestLogDetailDto>>> GetUserLogsAsync(string userId, int page = 1, int pageSize = 100, Guid? projectId = null);
    
    /// <summary>
    /// Gets logs for a specific endpoint
    /// </summary>
    Task<Result<List<RequestLogDetailDto>>> GetEndpointLogsAsync(Guid endpointId, int page = 1, int pageSize = 50);
    
    /// <summary>
    /// Gets a single log detail by ID
    /// </summary>
    Task<Result<RequestLogDetailDto>> GetByIdAsync(Guid logId);
    
    /// <summary>
    /// Deletes a single log (with permission check)
    /// </summary>
    Task<Result> DeleteLogAsync(Guid logId, string userId, bool isAdmin);
    
    /// <summary>
    /// Deletes all logs for a project (with permission check)
    /// </summary>
    Task<Result> DeleteProjectLogsAsync(Guid projectId, string userId, bool isAdmin);
    
    /// <summary>
    /// Deletes all logs (admin only)
    /// </summary>
    Task<Result> DeleteAllLogsAsync();
    
    /// <summary>
    /// Gets log count for dashboard
    /// </summary>
    Task<int> GetLogCountAsync(string? userId = null, bool isAdmin = false);
}

public class RequestLogDetailDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectSlug { get; set; } = string.Empty;
    public Guid? EndpointId { get; set; }
    public string? EndpointName { get; set; }
    public string? EndpointRoute { get; set; }
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
    public bool IsMatched { get; set; }
    public string? MatchedRoute { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

