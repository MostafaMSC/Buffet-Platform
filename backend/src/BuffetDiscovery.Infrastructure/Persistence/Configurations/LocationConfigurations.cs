using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuffetDiscovery.Infrastructure.Persistence.Configurations;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.Property(c => c.Code).HasMaxLength(2);
        builder.Property(c => c.CurrencyCode).HasMaxLength(3);
        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.SortOrder);
    }
}

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.HasOne(c => c.Country)
            .WithMany(c => c.Cities)
            .HasForeignKey(c => c.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.Slug).HasMaxLength(80);
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.HasIndex(c => c.SortOrder);
    }
}

public class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        builder.HasOne(a => a.City)
            .WithMany(c => c.Areas)
            .HasForeignKey(a => a.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.Slug).HasMaxLength(80);
        builder.HasIndex(a => new { a.CityId, a.Slug }).IsUnique();
        builder.HasIndex(a => a.SortOrder);
    }
}
