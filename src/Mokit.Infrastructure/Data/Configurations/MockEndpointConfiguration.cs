using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mokit.Domain.Entities;

namespace Mokit.Infrastructure.Data.Configurations;

public class MockEndpointConfiguration : IEntityTypeConfiguration<MockEndpoint>
{
    public void Configure(EntityTypeBuilder<MockEndpoint> builder)
    {
        builder.ToTable("MockEndpoints");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Route)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.RegexPattern)
            .HasMaxLength(1000);



        builder.HasMany(e => e.Responses)
            .WithOne(r => r.Endpoint)
            .HasForeignKey(r => r.EndpointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.ValidationRules)
            .WithOne(v => v.Endpoint)
            .HasForeignKey(v => v.EndpointId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ProjectId, e.Route, e.Method });
        builder.HasIndex(e => e.Order);
    }
}


