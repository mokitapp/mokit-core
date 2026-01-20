using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mokit.Domain.Entities;

namespace Mokit.Infrastructure.Data.Configurations;

public class RequestLogConfiguration : IEntityTypeConfiguration<RequestLog>
{
    public void Configure(EntityTypeBuilder<RequestLog> builder)
    {
        builder.ToTable("RequestLogs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Method)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(l => l.Path)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(l => l.ClientIp)
            .HasMaxLength(50);

        builder.Property(l => l.UserAgent)
            .HasMaxLength(500);

        builder.Property(l => l.MatchedRoute)
            .HasMaxLength(500);

        builder.Property(l => l.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasOne(l => l.Endpoint)
            .WithMany()
            .HasForeignKey(l => l.EndpointId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(l => l.CreatedAt);
        builder.HasIndex(l => l.ProjectId);
        builder.HasIndex(l => new { l.ProjectId, l.CreatedAt });
    }
}


