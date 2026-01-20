using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Mokit.Domain.Common;
using Mokit.Domain.Entities;

namespace Mokit.Infrastructure.Data;

public class MokitDbContext : IdentityDbContext<ApplicationUser>
{
    public MokitDbContext(DbContextOptions<MokitDbContext> options) : base(options)
    {
    }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<MockProject> MockProjects => Set<MockProject>();
    public DbSet<MockEndpoint> MockEndpoints => Set<MockEndpoint>();
    public DbSet<MockResponse> MockResponses => Set<MockResponse>();
    public DbSet<ValidationRule> ValidationRules => Set<ValidationRule>();
    public DbSet<RequestLog> RequestLogs => Set<RequestLog>();
    public DbSet<DynamicVariable> DynamicVariables => Set<DynamicVariable>();
    public DbSet<WebhookDefinition> WebhookDefinitions => Set<WebhookDefinition>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all configurations from assembly
        builder.ApplyConfigurationsFromAssembly(typeof(MokitDbContext).Assembly);

        // Configure BaseAuditableEntity audit fields for all entities
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            // Soft delete global query filter for all BaseEntity derivatives
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var propertyAccess = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var notDeleted = System.Linq.Expressions.Expression.Equal(propertyAccess, System.Linq.Expressions.Expression.Constant(false));
                var lambda = System.Linq.Expressions.Expression.Lambda(notDeleted, parameter);
                
                builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }

            // Configure CreatedByUser and UpdatedByUser navigation properties
            if (typeof(BaseAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                // CreatedBy field configuration
                var createdByProperty = entityType.FindProperty(nameof(BaseAuditableEntity.CreatedBy));
                if (createdByProperty != null)
                {
                    createdByProperty.SetMaxLength(450); // Match AspNetUsers.Id length
                }

                // UpdatedBy field configuration  
                var updatedByProperty = entityType.FindProperty(nameof(BaseAuditableEntity.UpdatedBy));
                if (updatedByProperty != null)
                {
                    updatedByProperty.SetMaxLength(450);
                }

                // Configure navigation properties
                builder.Entity(entityType.ClrType)
                    .HasOne(typeof(ApplicationUser), nameof(BaseAuditableEntity.CreatedByUser))
                    .WithMany()
                    .HasForeignKey(nameof(BaseAuditableEntity.CreatedBy))
                    .OnDelete(DeleteBehavior.Restrict);

                builder.Entity(entityType.ClrType)
                    .HasOne(typeof(ApplicationUser), nameof(BaseAuditableEntity.UpdatedByUser))
                    .WithMany()
                    .HasForeignKey(nameof(BaseAuditableEntity.UpdatedBy))
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }
    }
}
