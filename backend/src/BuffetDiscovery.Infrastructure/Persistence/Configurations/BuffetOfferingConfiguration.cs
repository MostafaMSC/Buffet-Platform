using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuffetDiscovery.Infrastructure.Persistence.Configurations;

public class BuffetOfferingConfiguration : IEntityTypeConfiguration<BuffetOffering>
{
    public void Configure(EntityTypeBuilder<BuffetOffering> builder)
    {
        builder.HasOne(o => o.Restaurant)
            .WithMany(r => r.Offerings)
            .HasForeignKey(o => o.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(o => o.Price).HasColumnType("numeric(10,0)");
        builder.HasIndex(o => new { o.RestaurantId, o.MealType });
    }
}
