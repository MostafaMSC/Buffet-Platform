using BuffetDiscovery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuffetDiscovery.Infrastructure.Persistence.Configurations;

public class MenuSectionConfiguration : IEntityTypeConfiguration<MenuSection>
{
    public void Configure(EntityTypeBuilder<MenuSection> builder)
    {
        builder.HasOne(s => s.Service)
            .WithMany(s => s.MenuSections)
            .HasForeignKey(s => s.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.Name).HasMaxLength(120);
        builder.Property(s => s.NameAr).HasMaxLength(120);
        builder.HasIndex(s => new { s.ServiceId, s.SortOrder });
    }
}

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.HasOne(i => i.MenuSection)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.MenuSectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(i => i.Name).HasMaxLength(160);
        builder.Property(i => i.NameAr).HasMaxLength(160);
        builder.HasIndex(i => new { i.MenuSectionId, i.SortOrder });
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasOne(r => r.Restaurant)
            .WithMany(r => r.Reviews)
            .HasForeignKey(r => r.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Service)
            .WithMany(s => s.Reviews)
            .HasForeignKey(r => r.ServiceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.Booking)
            .WithMany()
            .HasForeignKey(r => r.BookingId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(r => r.CustomerName).HasMaxLength(200);
        builder.HasIndex(r => r.RestaurantId);
        builder.HasIndex(r => r.ServiceId);
    }
}

public class SlotOverrideConfiguration : IEntityTypeConfiguration<SlotOverride>
{
    public void Configure(EntityTypeBuilder<SlotOverride> builder)
    {
        builder.HasOne(o => o.TimeSlot)
            .WithMany(s => s.Overrides)
            .HasForeignKey(o => o.TimeSlotId)
            .OnDelete(DeleteBehavior.Cascade);

        // One override per slot per date — the calendar edits in place rather than stacking.
        builder.HasIndex(o => new { o.TimeSlotId, o.Date }).IsUnique();
    }
}
