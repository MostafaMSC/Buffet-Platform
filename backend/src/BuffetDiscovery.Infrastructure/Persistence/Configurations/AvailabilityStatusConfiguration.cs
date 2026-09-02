using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuffetDiscovery.Infrastructure.Persistence.Configurations;

public class AvailabilityStatusConfiguration : IEntityTypeConfiguration<AvailabilityStatus>
{
    public void Configure(EntityTypeBuilder<AvailabilityStatus> builder)
    {
        builder.HasOne(a => a.Offering)
            .WithMany(o => o.AvailabilityStatuses)
            .HasForeignKey(a => a.OfferingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.OfferingId, a.Date }).IsUnique();
        builder.HasIndex(a => new { a.Date, a.IsActive });
    }
}
