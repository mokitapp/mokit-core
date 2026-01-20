using Microsoft.EntityFrameworkCore;
using Mokit.Application.Common;
using Mokit.Application.DTOs.Project;
using Mokit.Application.Helpers;
using Mokit.Application.Interfaces;
using Mokit.Application.Interfaces.Persistence;
using Mokit.Domain.Entities;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Services;

public class MockProjectService : IMockProjectService
{
    private readonly IUnitOfWork<MokitDbContext> _unitOfWork;
    private readonly ITeamService _teamService;

    public MockProjectService(
        IUnitOfWork<MokitDbContext> unitOfWork,
        ITeamService teamService)
    {
        _unitOfWork = unitOfWork;
        _teamService = teamService;
    }

    public async Task<Result<List<MockProjectDto>>> GetAllAsync(string userId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        var projects = await scope.Context.MockProjects
            .Include(p => p.Team)
                .ThenInclude(t => t!.Members)
            .Include(p => p.Endpoints)
            .Where(p => p.UserId == userId || 
                       (p.TeamId != null && p.Team!.Members.Any(m => m.UserId == userId && m.IsActive)))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Result<List<MockProjectDto>>.Success(projects.Select(MapToDto).ToList());
    }

    public async Task<Result<List<MockProjectDto>>> GetByTeamAsync(Guid teamId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        var projects = await scope.Context.MockProjects
            .Include(p => p.Endpoints)
            .Where(p => p.TeamId == teamId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
            
        return Result<List<MockProjectDto>>.Success(projects.Select(MapToDto).ToList());
    }

    public async Task<Result<MockProjectDto>> GetByIdAsync(Guid projectId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        var project = await scope.Context.MockProjects
            .Include(p => p.Team)
            .Include(p => p.Endpoints)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
        {
            return Result<MockProjectDto>.Failure("Project not found");
        }

        return Result<MockProjectDto>.Success(MapToDto(project));
    }

    public async Task<Result<MockProject>> GetByIdWithEndpointsAsync(Guid projectId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        var project = await scope.Context.MockProjects
            .Include(p => p.Endpoints)
                .ThenInclude(e => e.Responses)
            .Include(p => p.Endpoints)
                .ThenInclude(e => e.ValidationRules)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
        {
            return Result<MockProject>.Failure("Project not found");
        }

        return Result<MockProject>.Success(project);
    }

    public async Task<Result<MockProjectDto>> GetBySlugAsync(string slug, string? teamSlug = null)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        MockProject? project;
        
        if (string.IsNullOrEmpty(teamSlug))
        {
            project = await scope.Context.MockProjects
                .Include(p => p.Team)
                .Include(p => p.Endpoints)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.TeamId == null);
        }
        else
        {
            project = await scope.Context.MockProjects
                .Include(p => p.Team)
                .Include(p => p.Endpoints)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.Team != null && p.Team.Slug == teamSlug);
        }

        if (project == null)
        {
            return Result<MockProjectDto>.Failure("Project not found");
        }

        return Result<MockProjectDto>.Success(MapToDto(project));
    }

    public async Task<Result<MockProject>> GetBySlugWithEndpointsAsync(string slug, string? teamSlug = null)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        MockProject? project;
        
        if (string.IsNullOrEmpty(teamSlug))
        {
            project = await scope.Context.MockProjects
                .Include(p => p.Team)
                .Include(p => p.Endpoints)
                    .ThenInclude(e => e.Responses)
                .Include(p => p.Endpoints)
                    .ThenInclude(e => e.ValidationRules)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.TeamId == null);
        }
        else
        {
            project = await scope.Context.MockProjects
                .Include(p => p.Team)
                .Include(p => p.Endpoints)
                    .ThenInclude(e => e.Responses)
                .Include(p => p.Endpoints)
                    .ThenInclude(e => e.ValidationRules)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.Team != null && p.Team.Slug == teamSlug);
        }

        if (project == null)
        {
            return Result<MockProject>.Failure("Project not found");
        }

        return Result<MockProject>.Success(project);
    }

    public async Task<Result<MockProjectDto>> CreateAsync(string userId, CreateMockProjectDto dto)
    {
        var projectId = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            // Generate slug
            string slug;
            if (!string.IsNullOrEmpty(dto.Slug))
            {
                slug = SlugHelper.GenerateSlug(dto.Slug);
            }
            else
            {
                slug = SlugHelper.GenerateSlug(dto.Name);
            }

            // Ensure slug uniqueness within scope
            if (dto.TeamId.HasValue)
            {
                slug = SlugHelper.GenerateUniqueSlug(slug, s => 
                    scope.Context.MockProjects.Any(p => p.TeamId == dto.TeamId && p.Slug == s));
            }
            else
            {
                slug = SlugHelper.GenerateUniqueSlug(slug, s => 
                    scope.Context.MockProjects.Any(p => p.TeamId == null && p.Slug == s));
            }

            var project = new MockProject
            {
                Name = dto.Name,
                Slug = slug,
                Description = dto.Description,
                TeamId = dto.TeamId,
                UserId = dto.TeamId == null ? userId : null,
                DefaultDelay = dto.DefaultDelay,
                EnableCors = dto.EnableCors,
                EnableLogging = dto.EnableLogging,
                CreatedBy = userId
            };

            scope.Context.MockProjects.Add(project);
            return project.Id;
        });

        return await GetByIdAsync(projectId);
    }

    public async Task<Result<MockProjectDto>> UpdateAsync(Guid projectId, UpdateMockProjectDto dto)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var project = await scope.Context.MockProjects
                .Include(p => p.Team)
                .FirstOrDefaultAsync(p => p.Id == projectId);
                
            if (project == null)
            {
                return (false, "Project not found");
            }

            // Regenerate slug if name changed
            if (project.Name != dto.Name)
            {
                var newSlug = SlugHelper.GenerateSlug(dto.Name);
                if (project.TeamId.HasValue)
                {
                    newSlug = SlugHelper.GenerateUniqueSlug(newSlug, s => 
                        scope.Context.MockProjects.Any(p => p.Id != projectId && p.TeamId == project.TeamId && p.Slug == s));
                }
                else
                {
                    newSlug = SlugHelper.GenerateUniqueSlug(newSlug, s => 
                        scope.Context.MockProjects.Any(p => p.Id != projectId && p.TeamId == null && p.Slug == s));
                }
                project.Slug = newSlug;
            }

            project.Name = dto.Name;
            project.Description = dto.Description;
            project.IsActive = dto.IsActive;
            project.DefaultDelay = dto.DefaultDelay;
            project.EnableCors = dto.EnableCors;
            project.EnableLogging = dto.EnableLogging;
            project.EnableLatencySimulation = dto.EnableLatencySimulation;
            project.GlobalLatencyMin = dto.GlobalLatencyMin;
            project.GlobalLatencyMax = dto.GlobalLatencyMax;
            project.EnableJwtValidation = dto.EnableJwtValidation;
            project.JwtSecret = dto.JwtSecret;
            project.JwtIssuer = dto.JwtIssuer;
            project.JwtAudience = dto.JwtAudience;
            project.UpdatedAt = DateTime.UtcNow;

            return (true, (string?)null);
        });

        if (!result.Item1)
        {
            return Result<MockProjectDto>.Failure(result.Item2 ?? "Update failed");
        }

        return await GetByIdAsync(projectId);
    }

    public async Task<Result> DeleteAsync(Guid projectId)
    {
        var result = await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            var project = await scope.Context.MockProjects.FindAsync(projectId);
            if (project == null)
            {
                return (false, "Project not found");
            }

            scope.Context.MockProjects.Remove(project);
            return (true, (string?)null);
        });

        if (!result.Item1)
        {
            return Result.Failure(result.Item2 ?? "Delete failed");
        }

        return Result.Success();
    }

    public async Task<bool> IsSlugAvailableAsync(string slug, Guid? teamId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        if (teamId.HasValue)
        {
            return !await scope.Context.MockProjects.AnyAsync(p => p.TeamId == teamId && p.Slug == slug);
        }
        return !await scope.Context.MockProjects.AnyAsync(p => p.TeamId == null && p.Slug == slug);
    }

    public async Task<bool> CanUserAccessProjectAsync(Guid projectId, string userId)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();
        
        var project = await scope.Context.MockProjects
            .Include(p => p.Team)
            .ThenInclude(t => t!.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null) return false;

        // Personal project
        if (project.UserId == userId) return true;

        // Team project
        if (project.TeamId != null)
        {
            return project.Team?.Members.Any(m => m.UserId == userId && m.IsActive) ?? false;
        }

        return false;
    }

    private static MockProjectDto MapToDto(MockProject p)
    {
        return new MockProjectDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            Description = p.Description,
            IsActive = p.IsActive,
            TeamId = p.TeamId,
            TeamName = p.Team?.Name,
            TeamSlug = p.Team?.Slug,
            UserId = p.UserId,
            EndpointCount = p.Endpoints.Count,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            DefaultDelay = p.DefaultDelay,
            EnableCors = p.EnableCors,
            EnableLogging = p.EnableLogging,
            EnableJwtValidation = p.EnableJwtValidation
        };
    }
}
