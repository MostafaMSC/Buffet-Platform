using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Settings;

/// Only the restaurant-editable fields (per the founder's decision: cancellation cutoff and
/// overbooking tolerance are restaurant-editable from day one, consistent with slot capacity
/// already being restaurant-editable). IsFoundingRestaurant, FeaturedScore and ReferredBy
/// are admin-only — see Features/Admin/UpdateRestaurantBookingFlagsCommand.
public record UpdateRestaurantSettingsCommand(
    int CancellationCutoffMinutes,
    int WaitlistOfferWindowMinutes,
    int OverbookingTolerancePercent
) : IRequest;

public class UpdateRestaurantSettingsCommandValidator : AbstractValidator<UpdateRestaurantSettingsCommand>
{
    public UpdateRestaurantSettingsCommandValidator()
    {
        RuleFor(x => x.CancellationCutoffMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WaitlistOfferWindowMinutes).GreaterThan(0);
        RuleFor(x => x.OverbookingTolerancePercent).InclusiveBetween(0, 100);
    }
}

public class UpdateRestaurantSettingsCommandHandler(
    IRestaurantSettingsRepository settingsRepo,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateRestaurantSettingsCommand>
{
    public async Task Handle(UpdateRestaurantSettingsCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var settings = await settingsRepo.GetOrCreateAsync(restaurantId, ct);

        settings.CancellationCutoffMinutes = request.CancellationCutoffMinutes;
        settings.WaitlistOfferWindowMinutes = request.WaitlistOfferWindowMinutes;
        settings.OverbookingTolerancePercent = request.OverbookingTolerancePercent;

        await unitOfWork.SaveChangesAsync(ct);
    }
}
