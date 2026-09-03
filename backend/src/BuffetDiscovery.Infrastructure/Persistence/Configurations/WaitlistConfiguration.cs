using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuffetDiscovery.Infrastructure.Persistence.Configurations;

public class WaitlistConfiguration : IEntityTypeConfiguration<Waitlist>
{
    public void Configure(EntityTypeBuilder<Waitlist> builder)
    {
        builder.HasOne(w => w.Service)
            .WithMany()
            .HasForeignKey(w => w.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.TimeSlot)
            .WithMany(s => s.WaitlistEntries)
            .HasForeignKey(w => w.TimeSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(w => new { w.TimeSlotId, w.Date, w.Status, w.Position });
        builder.HasIndex(w => new { w.ServiceId, w.Date, w.Status, w.Position });
        builder.HasIndex(w => w.CustomerPhone);
    }
}
