using Mokit.Domain.Entities;

namespace Mokit.Application.Interfaces;

public interface IMockDataProvider
{
    Task<MockProject?> GetProjectByTeamSlashProjectSlugAsync(string teamSlug, string projectSlug);
    Task<MockProject?> GetProjectBySlugAsync(string projectSlug);
    Task LogRequestAsync(RequestLog log);
}
