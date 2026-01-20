using Mokit.Application.Common;
using Mokit.Application.DTOs.Project;
using Mokit.Domain.Entities;

namespace Mokit.Application.Interfaces;

public interface IMockProjectService
{
    Task<Result<List<MockProjectDto>>> GetAllAsync(string userId);
    Task<Result<List<MockProjectDto>>> GetByTeamAsync(Guid teamId);
    Task<Result<MockProjectDto>> GetByIdAsync(Guid projectId);
    Task<Result<MockProject>> GetByIdWithEndpointsAsync(Guid projectId);
    Task<Result<MockProjectDto>> GetBySlugAsync(string slug, string? teamSlug = null);
    Task<Result<MockProject>> GetBySlugWithEndpointsAsync(string slug, string? teamSlug = null);
    Task<Result<MockProjectDto>> CreateAsync(string userId, CreateMockProjectDto dto);
    Task<Result<MockProjectDto>> UpdateAsync(Guid projectId, UpdateMockProjectDto dto);
    Task<Result> DeleteAsync(Guid projectId);
    Task<bool> IsSlugAvailableAsync(string slug, Guid? teamId);
    Task<bool> CanUserAccessProjectAsync(Guid projectId, string userId);
}
