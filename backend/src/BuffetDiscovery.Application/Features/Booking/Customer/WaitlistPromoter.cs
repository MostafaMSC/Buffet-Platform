using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;

namespace BuffetDiscovery.Application.Features.Booking.Customer;

/// Lazily expires stale waitlist offers and promotes the next person in line whenever the
/// queue is touched (on cancellation, or opportunistically when availability is read) —
/// same pattern as AvailabilityStatus's per-date lazy materialization in Phase 1. No
/// background job infrastructure exists in this codebase, so this stays read/write-triggered
/// rather than time-triggered.
public class WaitlistPromoter(
    IWaitlistRepository waitlistRepo,
    IBookingRepository bookingRepo,
    IRestaurantSettingsRepository settingsRepo,
    INotificationService notifications)
{
    public async Task ExpireAndPromoteAsync(int? timeSlotId, int serviceId, int restaurantId, DateOnly date, int capacity, CancellationToken ct)
    {
        var queue = await waitlistRepo.GetQueueAsync(timeSlotId, serviceId, date, ct);
        if (queue.Count == 0) return;

        var settings = await settingsRepo.GetOrCreateAsync(restaurantId, ct);
        var now = DateTime.UtcNow;

        var activeOffer = queue.FirstOrDefault(w => w.Status == WaitlistStatus.Offered);
        if (activeOffer is not null)
        {
            var expiresAt = activeOffer.NotifiedAt!.Value.AddMinutes(settings.WaitlistOfferWindowMinutes);
            if (now < expiresAt)
            {
                return; // offer still open, nothing to do
            }

            activeOffer.Status = WaitlistStatus.Expired;
            queue.Remove(activeOffer);
        }

        var bookedPartySize = await bookingRepo.GetBookedPartySizeAsync(timeSlotId, serviceId, date, ct);
        var next = queue
            .Where(w => w.Status == WaitlistStatus.Waiting)
            .OrderBy(w => w.Position)
            .FirstOrDefault(w => bookedPartySize + w.PartySize <= capacity);

        if (next is not null)
        {
            next.Status = WaitlistStatus.Offered;
            next.NotifiedAt = now;
            await notifications.NotifyCustomerAsync(
                next.CustomerPhone,
                $"A spot opened up! You have {settings.WaitlistOfferWindowMinutes} minutes to confirm your booking.",
                ct);
        }
    }
}
