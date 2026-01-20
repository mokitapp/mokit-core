using Microsoft.EntityFrameworkCore;
using Mokit.Application.Interfaces;
using Mokit.Application.Interfaces.Persistence;
using Mokit.Domain.Entities;
using Mokit.Infrastructure.Data;

namespace Mokit.Infrastructure.Services;

public class MockDataProvider : IMockDataProvider
{
    private readonly IUnitOfWork<MokitDbContext> _unitOfWork;

    public MockDataProvider(IUnitOfWork<MokitDbContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MockProject?> GetProjectByTeamSlashProjectSlugAsync(string teamSlug, string projectSlug)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();

        var team = await scope.Context.Teams
            .FirstOrDefaultAsync(t => t.Slug == teamSlug && t.IsActive);

        if (team == null) return null;

        return await scope.Context.MockProjects
            .Include(p => p.Endpoints)
                .ThenInclude(e => e.Responses)
            .Include(p => p.Endpoints)
                .ThenInclude(e => e.ValidationRules)
            .Include(p => p.Endpoints)
                .ThenInclude(e => e.Webhooks)
            .FirstOrDefaultAsync(p => p.TeamId == team.Id && p.Slug == projectSlug && p.IsActive);
    }

    public async Task<MockProject?> GetProjectBySlugAsync(string projectSlug)
    {
        await using var scope = await _unitOfWork.CreateScopeAsync();

        return await scope.Context.MockProjects
            .Include(p => p.Endpoints)
                .ThenInclude(e => e.Responses)
            .Include(p => p.Endpoints)
                .ThenInclude(e => e.ValidationRules)
            .Include(p => p.Endpoints)
                .ThenInclude(e => e.Webhooks)
            .FirstOrDefaultAsync(p => p.TeamId == null && p.Slug == projectSlug && p.IsActive);
    }

    public async Task LogRequestAsync(RequestLog log)
    {
        await _unitOfWork.ExecuteTransactionAsync(async scope =>
        {
            scope.Context.RequestLogs.Add(log);
        });
    }
}
