using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuffetDiscovery.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasOne(b => b.Offering)
            .WithMany(o => o.Bookings)
            .HasForeignKey(b => b.OfferingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.TimeSlot)
            .WithMany(s => s.Bookings)
            .HasForeignKey(b => b.TimeSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(b => b.ConfirmationCode).HasMaxLength(12);
        builder.HasIndex(b => b.ConfirmationCode).IsUnique();
        builder.HasIndex(b => b.CustomerPhone);
        builder.HasIndex(b => new { b.TimeSlotId, b.Date, b.Status });
        builder.HasIndex(b => new { b.OfferingId, b.Date, b.Status });
    }
}
