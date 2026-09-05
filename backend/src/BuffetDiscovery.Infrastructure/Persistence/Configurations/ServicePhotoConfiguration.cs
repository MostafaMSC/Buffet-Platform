using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuffetDiscovery.Infrastructure.Persistence.Configurations;

public class ServicePhotoConfiguration : IEntityTypeConfiguration<ServicePhoto>
{
    public void Configure(EntityTypeBuilder<ServicePhoto> builder)
    {
        builder.HasOne(p => p.Service)
            .WithMany(s => s.Photos)
            .HasForeignKey(p => p.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
