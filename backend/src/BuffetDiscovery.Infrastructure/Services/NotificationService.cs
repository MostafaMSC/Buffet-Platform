using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using BuffetDiscovery.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace BuffetDiscovery.Infrastructure.Services;

public class NotificationService(AppDbContext db, ILogger<NotificationService> logger) : INotificationService
{
    public Task NotifyRestaurantAsync(int restaurantId, string message, string? messageAr, CancellationToken ct)
    {
        db.Notifications.Add(new Notification
        {
            RestaurantId = restaurantId,
            Message = message,
            MessageAr = messageAr
        });
        // Intentionally not calling SaveChanges here — the calling handler's own
        // unitOfWork.SaveChangesAsync() persists this alongside the booking/cancellation
        // it's reporting on, in the same transaction.
        return Task.CompletedTask;
    }

    public Task NotifyCustomerAsync(string phone, string message, CancellationToken ct)
    {
        // No customer delivery channel exists yet (no accounts, no SMS/WhatsApp wired up) —
        // this is the plug-in point for that later. For now the customer sees current status
        // via their booking badge / phone lookup instead.
        logger.LogInformation("Customer notification (not delivered, no channel wired up) to {Phone}: {Message}", phone, message);
        return Task.CompletedTask;
    }
}
