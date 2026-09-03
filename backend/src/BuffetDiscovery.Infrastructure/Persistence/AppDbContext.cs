using System.Reflection;
using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServicePhoto> ServicePhotos => Set<ServicePhoto>();
    public DbSet<MenuSection> MenuSections => Set<MenuSection>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<AvailabilityStatus> AvailabilityStatuses => Set<AvailabilityStatus>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<SlotOverride> SlotOverrides => Set<SlotOverride>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Waitlist> WaitlistEntries => Set<Waitlist>();
    public DbSet<RestaurantSettings> RestaurantSettings => Set<RestaurantSettings>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
