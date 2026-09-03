using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuffetDiscovery.Infrastructure.Persistence.Configurations;

public class TimeSlotConfiguration : IEntityTypeConfiguration<TimeSlot>
{
    public void Configure(EntityTypeBuilder<TimeSlot> builder)
    {
        builder.HasOne(s => s.Offering)
            .WithMany(o => o.TimeSlots)
            .HasForeignKey(s => s.OfferingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.OfferingId, s.IsDeleted });
    }
}
