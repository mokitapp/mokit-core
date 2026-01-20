using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mokit.Domain.Entities;

namespace Mokit.Infrastructure.Data.Configurations;

public class ValidationRuleConfiguration : IEntityTypeConfiguration<ValidationRule>
{
    public void Configure(EntityTypeBuilder<ValidationRule> builder)
    {
        builder.ToTable("ValidationRules");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.ParameterName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.DataType)
            .HasMaxLength(50);

        builder.Property(v => v.RegexPattern)
            .HasMaxLength(500);

        builder.Property(v => v.MinValue)
            .HasMaxLength(100);

        builder.Property(v => v.MaxValue)
            .HasMaxLength(100);

        builder.Property(v => v.DefaultValue)
            .HasMaxLength(500);

        builder.Property(v => v.ErrorMessage)
            .HasMaxLength(500);

        builder.HasIndex(v => new { v.EndpointId, v.ParameterName, v.Location })
            .IsUnique();
    }
}


