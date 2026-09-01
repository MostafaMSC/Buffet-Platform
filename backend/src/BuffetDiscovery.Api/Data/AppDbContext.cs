using BuffetDiscovery.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<BuffetOffering> Offerings => Set<BuffetOffering>();
    public DbSet<OfferingPhoto> OfferingPhotos => Set<OfferingPhoto>();
    public DbSet<AvailabilityStatus> AvailabilityStatuses => Set<AvailabilityStatus>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Area>(e =>
        {
            e.HasIndex(a => a.SortOrder);
        });

        modelBuilder.Entity<Restaurant>(e =>
        {
            e.HasOne(r => r.Area)
                .WithMany(a => a.Restaurants)
                .HasForeignKey(r => r.AreaId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(r => r.Status);
            e.Property(r => r.Name).HasMaxLength(200);
            e.Property(r => r.NameAr).HasMaxLength(200);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.PhoneNumber).IsUnique();
            e.HasOne(u => u.Restaurant)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BuffetOffering>(e =>
        {
            e.HasOne(o => o.Restaurant)
                .WithMany(r => r.Offerings)
                .HasForeignKey(o => o.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Property(o => o.Price).HasColumnType("numeric(10,0)");
            e.HasIndex(o => new { o.RestaurantId, o.MealType });
        });

        modelBuilder.Entity<OfferingPhoto>(e =>
        {
            e.HasOne(p => p.Offering)
                .WithMany(o => o.Photos)
                .HasForeignKey(p => p.OfferingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AvailabilityStatus>(e =>
        {
            e.HasOne(a => a.Offering)
                .WithMany(o => o.AvailabilityStatuses)
                .HasForeignKey(a => a.OfferingId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(a => new { a.OfferingId, a.Date }).IsUnique();
            e.HasIndex(a => new { a.Date, a.IsActive });
        });
    }
}
