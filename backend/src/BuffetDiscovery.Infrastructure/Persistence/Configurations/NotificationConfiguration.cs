using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuffetDiscovery.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasOne(n => n.Restaurant)
            .WithMany()
            .HasForeignKey(n => n.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.RestaurantId, n.IsRead, n.CreatedAt });
    }
}
