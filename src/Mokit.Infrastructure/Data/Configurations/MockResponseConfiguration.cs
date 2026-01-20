using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mokit.Domain.Entities;

namespace Mokit.Infrastructure.Data.Configurations;

public class MockResponseConfiguration : IEntityTypeConfiguration<MockResponse>
{
    public void Configure(EntityTypeBuilder<MockResponse> builder)
    {
        builder.ToTable("MockResponses");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Condition)
            .HasMaxLength(500);

        builder.Property(r => r.ConditionExpression)
            .HasMaxLength(2000);

        builder.Property(r => r.FilePath)
            .HasMaxLength(500);

        builder.Property(r => r.FileName)
            .HasMaxLength(200);

        builder.HasIndex(r => new { r.EndpointId, r.Order });
        builder.HasIndex(r => r.IsDefault);
    }
}


