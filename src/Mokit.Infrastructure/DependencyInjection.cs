using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mokit.Application.Interfaces;
using Mokit.Application.Interfaces.Persistence;
using Mokit.Application.Interfaces.Repositories;
using Mokit.Domain.Entities;
using Mokit.Infrastructure.Data;
using Mokit.Infrastructure.Persistence.Repositories;
using Mokit.Infrastructure.Persistence.UnitOfWork;
using Mokit.Infrastructure.Services;

namespace Mokit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Use DbContext Factory - creates isolated instances for each operation
        services.AddDbContextFactory<MokitDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure();
            });
        });

        // Register scoped DbContext from factory - for Identity and legacy code
        services.AddScoped<MokitDbContext>(sp => 
            sp.GetRequiredService<IDbContextFactory<MokitDbContext>>().CreateDbContext());

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;

            // User settings
            options.User.RequireUniqueEmail = true;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<MokitDbContext>()
        .AddDefaultTokenProviders()
        .AddClaimsPrincipalFactory<Identity.MokitClaimsPrincipalFactory>();

        // Register services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IMockProjectService, MockProjectService>();
        services.AddScoped<IMockEndpointService, MockEndpointService>();
        services.AddScoped<IMockResponseService, MockResponseService>();
        services.AddScoped<IRequestLogService, RequestLogService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IMockDataProvider, MockDataProvider>();

        // Register persistence services (factory-based UnitOfWork for Blazor Server)
        services.AddScoped<IUnitOfWork<MokitDbContext>, Persistence.UnitOfWork.UnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IUnitOfWork<MokitDbContext>>());

        // Register repositories
        services.AddScoped<IMockProjectRepository, MockProjectRepository>();
        services.AddScoped<IMockEndpointRepository, MockEndpointRepository>();
        services.AddScoped<IMockResponseRepository, MockResponseRepository>();
        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IRequestLogRepository, RequestLogRepository>();
        services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
        services.AddScoped<IValidationRuleRepository, ValidationRuleRepository>();
        services.AddScoped<IWebhookDefinitionRepository, WebhookDefinitionRepository>();
        services.AddScoped<IDynamicVariableRepository, DynamicVariableRepository>();

        return services;
    }
}
