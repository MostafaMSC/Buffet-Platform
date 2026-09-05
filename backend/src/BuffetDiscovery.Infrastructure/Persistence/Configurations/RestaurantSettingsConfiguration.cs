using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuffetDiscovery.Infrastructure.Persistence.Configurations;

public class RestaurantSettingsConfiguration : IEntityTypeConfiguration<RestaurantSettings>
{
    public void Configure(EntityTypeBuilder<RestaurantSettings> builder)
    {
        builder.HasOne(s => s.Restaurant)
            .WithOne(r => r.Settings)
            .HasForeignKey<RestaurantSettings>(s => s.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.RestaurantId).IsUnique();

        builder.HasOne(s => s.ReferredBy)
            .WithMany()
            .HasForeignKey(s => s.ReferredByRestaurantId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
