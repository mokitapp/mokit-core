using Microsoft.EntityFrameworkCore;
using Mokit.Application.Common;
using Mokit.Application.Interfaces;
using Mokit.Application.Interfaces.Persistence;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Services;

public class RequestLogService : IRequestLogService
{
    private readonly IUnitOfWork<MokitDbContext> _unitOfWork;

    public RequestLogService(IUnitOfWork<MokitDbContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<RequestLogDetailDto>>> GetAllLogsAsync(int page = 1, int pageSize = 100, Guid? projectId = null)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();

        var query = scope.Context.RequestLogs
            .Include(l => l.Project)
            .Include(l => l.Endpoint)
            .AsQueryable();

        if (projectId.HasValue)
        {
            query = query.Where(l => l.ProjectId == projectId.Value);
        }

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = logs.Select(l => MapToDto(l)).ToList();
        return Result<List<RequestLogDetailDto>>.Success(dtos);
    }

    public async Task<Result<List<RequestLogDetailDto>>> GetUserLogsAsync(string userId, int page = 1, int pageSize = 100, Guid? projectId = null)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();

        // Get user's personal project IDs
        var personalProjectIds = await scope.Context.MockProjects
            .Where(p => p.UserId == userId && p.TeamId == null)
            .Select(p => p.Id)
            .ToListAsync();

        // Get user's team project IDs
        var teamIds = await scope.Context.TeamMembers
            .Where(tm => tm.UserId == userId)
            .Select(tm => tm.TeamId)
            .ToListAsync();

        var teamProjectIds = await scope.Context.MockProjects
            .Where(p => p.TeamId.HasValue && teamIds.Contains(p.TeamId.Value))
            .Select(p => p.Id)
            .ToListAsync();

        var allowedProjectIds = personalProjectIds.Concat(teamProjectIds).ToHashSet();

        var query = scope.Context.RequestLogs
            .Include(l => l.Project)
            .Include(l => l.Endpoint)
            .Where(l => allowedProjectIds.Contains(l.ProjectId));

        if (projectId.HasValue)
        {
            query = query.Where(l => l.ProjectId == projectId.Value);
        }

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = logs.Select(l => MapToDto(l)).ToList();
        return Result<List<RequestLogDetailDto>>.Success(dtos);
    }

    public async Task<Result<List<RequestLogDetailDto>>> GetEndpointLogsAsync(Guid endpointId, int page = 1, int pageSize = 50)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();

        var logs = await scope.Context.RequestLogs
            .Include(l => l.Project)
            .Include(l => l.Endpoint)
            .Where(l => l.EndpointId == endpointId)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = logs.Select(l => MapToDto(l)).ToList();
        return Result<List<RequestLogDetailDto>>.Success(dtos);
    }

    public async Task<Result<RequestLogDetailDto>> GetByIdAsync(Guid logId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();

        var log = await scope.Context.RequestLogs
            .Include(l => l.Project)
            .Include(l => l.Endpoint)
            .FirstOrDefaultAsync(l => l.Id == logId);

        if (log == null)
        {
            return Result<RequestLogDetailDto>.Failure("Log not found");
        }

        return Result<RequestLogDetailDto>.Success(MapToDto(log));
    }

    public async Task<Result> DeleteLogAsync(Guid logId, string userId, bool isAdmin)
    {
        // First check if log exists and user has permission (read-only)
        await using var readScope = await _unitOfWork.CreateScopeAsync();
        
        var log = await readScope.Context.RequestLogs
            .Include(l => l.Project)
            .FirstOrDefaultAsync(l => l.Id == logId);

        if (log == null)
        {
            return Result.Failure("Log not found");
        }

        // Check permission
        if (!isAdmin)
        {
            var hasPermission = await HasProjectPermissionAsync(readScope.Context, log.ProjectId, userId);
            if (!hasPermission)
            {
                return Result.Failure("You don't have permission to delete this log");
            }
        }

        // Now delete in a transaction
        await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var logToDelete = await scope.Context.RequestLogs.FindAsync(logId);
            if (logToDelete != null)
            {
                scope.Context.RequestLogs.Remove(logToDelete);
            }
        });

        return Result.Success();
    }

    public async Task<Result> DeleteProjectLogsAsync(Guid projectId, string userId, bool isAdmin)
    {
        // First check permission (read-only)
        if (!isAdmin)
        {
            await using var readScope = await _unitOfWork.CreateScopeAsync();
            var hasPermission = await HasProjectPermissionAsync(readScope.Context, projectId, userId);
            if (!hasPermission)
            {
                return Result.Failure("You don't have permission to delete logs for this project");
            }
        }

        // Now delete in a transaction
        await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var logs = await scope.Context.RequestLogs
                .Where(l => l.ProjectId == projectId)
                .ToListAsync();

            scope.Context.RequestLogs.RemoveRange(logs);
        });

        return Result.Success();
    }

    public async Task<Result> DeleteAllLogsAsync()
    {
        await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var logs = await scope.Context.RequestLogs.ToListAsync();
            scope.Context.RequestLogs.RemoveRange(logs);
        });

        return Result.Success();
    }

    public async Task<int> GetLogCountAsync(string? userId = null, bool isAdmin = false)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();

        if (isAdmin || string.IsNullOrEmpty(userId))
        {
            return await scope.Context.RequestLogs.CountAsync();
        }

        // Get user's allowed project IDs
        var personalProjectIds = await scope.Context.MockProjects
            .Where(p => p.UserId == userId && p.TeamId == null)
            .Select(p => p.Id)
            .ToListAsync();

        var teamIds = await scope.Context.TeamMembers
            .Where(tm => tm.UserId == userId)
            .Select(tm => tm.TeamId)
            .ToListAsync();

        var teamProjectIds = await scope.Context.MockProjects
            .Where(p => p.TeamId.HasValue && teamIds.Contains(p.TeamId.Value))
            .Select(p => p.Id)
            .ToListAsync();

        var allowedProjectIds = personalProjectIds.Concat(teamProjectIds).ToHashSet();

        return await scope.Context.RequestLogs
            .Where(l => allowedProjectIds.Contains(l.ProjectId))
            .CountAsync();
    }

    private static async Task<bool> HasProjectPermissionAsync(MokitDbContext context, Guid projectId, string userId)
    {
        var project = await context.MockProjects
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
        {
            return false;
        }

        // Check if user owns the project
        if (project.UserId == userId && project.TeamId == null)
        {
            return true;
        }

        // Check if user is a member of the project's team
        if (project.TeamId.HasValue)
        {
            var isMember = await context.TeamMembers
                .AnyAsync(tm => tm.TeamId == project.TeamId.Value && tm.UserId == userId);
            return isMember;
        }

        return false;
    }

    private static RequestLogDetailDto MapToDto(Domain.Entities.RequestLog l)
    {
        return new RequestLogDetailDto
        {
            Id = l.Id,
            ProjectId = l.ProjectId,
            ProjectName = l.Project?.Name ?? "Unknown",
            ProjectSlug = l.Project?.Slug ?? "",
            EndpointId = l.EndpointId,
            EndpointName = l.Endpoint?.Name,
            EndpointRoute = l.Endpoint?.Route,
            Method = l.Method,
            Path = l.Path,
            QueryString = l.QueryString,
            RequestHeaders = l.RequestHeaders,
            RequestBody = l.RequestBody,
            ResponseStatusCode = l.ResponseStatusCode,
            ResponseHeaders = l.ResponseHeaders,
            ResponseBody = l.ResponseBody,
            DurationMs = l.DurationMs,
            ClientIp = l.ClientIp,
            UserAgent = l.UserAgent,
            IsMatched = l.IsMatched,
            MatchedRoute = l.MatchedRoute,
            ErrorMessage = l.ErrorMessage,
            CreatedAt = l.CreatedAt
        };
    }
}
