using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuffetDiscovery.Infrastructure.Persistence.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.HasOne(s => s.Restaurant)
            .WithMany(r => r.Services)
            .HasForeignKey(s => s.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.Name).HasMaxLength(200);
        builder.Property(s => s.NameAr).HasMaxLength(200);

        builder.Property(s => s.PricePerAdult).HasColumnType("numeric(10,0)");
        builder.Property(s => s.PricePerChild).HasColumnType("numeric(10,0)");
        builder.Property(s => s.PackagePrice).HasColumnType("numeric(10,0)");

        // Search filters on these together constantly: only live services of a given type
        // count, and the type/meal pair narrows most queries before anything else runs.
        builder.HasIndex(s => new { s.ServiceType, s.Status, s.IsDeleted });
        builder.HasIndex(s => new { s.RestaurantId, s.ServiceType });
    }
}
