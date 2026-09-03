using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuffetDiscovery.Infrastructure.Persistence.Configurations;

public class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot>
{
    public void Configure(EntityTypeBuilder<TimeSlot> builder)
    {
        builder.HasOne(s => s.Service)
            .WithMany(o => o.TimeSlots)
            .HasForeignKey(s => s.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.ServiceId, s.IsDeleted });
    }
}
