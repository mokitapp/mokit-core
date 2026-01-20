using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Mokit.Domain.Entities;

namespace Mokit.Infrastructure.Data.Configurations;

public class WebhookDefinitionConfiguration : IEntityTypeConfiguration<WebhookDefinition>
{
    public void Configure(EntityTypeBuilder<WebhookDefinition> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Url)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(x => x.Method)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.DelayMs)
            .HasDefaultValue(0);

        builder.HasOne(x => x.Endpoint)
            .WithMany(x => x.Webhooks)
            .HasForeignKey(x => x.EndpointId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
