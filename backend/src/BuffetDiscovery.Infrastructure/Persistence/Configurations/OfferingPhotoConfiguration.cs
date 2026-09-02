using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuffetDiscovery.Infrastructure.Persistence.Configurations;

public class OfferingPhotoConfiguration : IEntityTypeConfiguration<OfferingPhoto>
{
    public void Configure(EntityTypeBuilder<OfferingPhoto> builder)
    {
        builder.HasOne(p => p.Offering)
            .WithMany(o => o.Photos)
            .HasForeignKey(p => p.OfferingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
