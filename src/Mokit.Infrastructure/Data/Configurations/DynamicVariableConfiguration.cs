using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mokit.Domain.Entities;

namespace Mokit.Infrastructure.Data.Configurations;

public class DynamicVariableConfiguration : IEntityTypeConfiguration<DynamicVariable>
{
    public void Configure(EntityTypeBuilder<DynamicVariable> builder)
    {
        builder.ToTable("DynamicVariables");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Description)
            .HasMaxLength(500);

        builder.Property(v => v.Expression)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(v => v.DefaultValue)
            .HasMaxLength(1000);

        builder.Property(v => v.CurrentValue)
            .HasMaxLength(1000);

        builder.HasIndex(v => new { v.ProjectId, v.Name })
            .IsUnique();
    }
}


