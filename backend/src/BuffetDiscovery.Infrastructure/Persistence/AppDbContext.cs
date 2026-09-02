using System.Reflection;
using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuffetDiscovery.Infrastructure.Persistence;

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
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
