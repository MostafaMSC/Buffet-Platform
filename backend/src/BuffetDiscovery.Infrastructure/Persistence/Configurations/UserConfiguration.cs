using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuffetDiscovery.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(u => u.PhoneNumber).IsUnique();
        builder.HasOne(u => u.Restaurant)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
