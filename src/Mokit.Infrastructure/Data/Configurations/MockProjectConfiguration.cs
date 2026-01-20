using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mokit.Domain.Entities;

namespace Mokit.Infrastructure.Data.Configurations;

public class MockProjectConfiguration : IEntityTypeConfiguration<MockProject>
{
    public void Configure(EntityTypeBuilder<MockProject> builder)
    {
        builder.ToTable("MockProjects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.UserId)
            .HasMaxLength(450);

        builder.Property(p => p.JwtSecret)
            .HasMaxLength(500);

        builder.Property(p => p.JwtIssuer)
            .HasMaxLength(200);

        builder.Property(p => p.JwtAudience)
            .HasMaxLength(200);

        builder.HasMany(p => p.Endpoints)
            .WithOne(e => e.Project)
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.RequestLogs)
            .WithOne(l => l.Project)
            .HasForeignKey(l => l.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.DynamicVariables)
            .WithOne(v => v.Project)
            .HasForeignKey(v => v.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique slug within scope (team or personal)
        // Personal projects: unique slug among all personal projects
        // Team projects: unique slug within the team
        builder.HasIndex(p => new { p.TeamId, p.Slug })
            .IsUnique();

        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.Slug);
    }
}
