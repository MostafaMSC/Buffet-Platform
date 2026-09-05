using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuffetDiscovery.Infrastructure.Persistence.Configurations;

public class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.HasOne(r => r.Area)
            .WithMany(a => a.Restaurants)
            .HasForeignKey(r => r.AreaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.Status);
        builder.Property(r => r.Name).HasMaxLength(200);
        builder.Property(r => r.NameAr).HasMaxLength(200);
    }
}
