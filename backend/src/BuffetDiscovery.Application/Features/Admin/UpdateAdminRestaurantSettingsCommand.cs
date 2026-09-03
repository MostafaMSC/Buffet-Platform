using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Admin;

public record UpdateAdminRestaurantSettingsCommand(
    int RestaurantId,
    int OverbookingTolerancePercent,
    bool IsFoundingRestaurant,
    int FeaturedScore,
    int? ReferredByRestaurantId
) : IRequest;

public class UpdateAdminRestaurantSettingsCommandValidator : AbstractValidator<UpdateAdminRestaurantSettingsCommand>
{
    public UpdateAdminRestaurantSettingsCommandValidator()
    {
        RuleFor(x => x.OverbookingTolerancePercent).InclusiveBetween(0, 100);
        RuleFor(x => x.FeaturedScore).GreaterThanOrEqualTo(0);
    }
}

public class UpdateAdminRestaurantSettingsCommandHandler(
    IRestaurantSettingsRepository settingsRepo,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateAdminRestaurantSettingsCommand>
{
    public async Task Handle(UpdateAdminRestaurantSettingsCommand request, CancellationToken ct)
    {
        if (request.ReferredByRestaurantId == request.RestaurantId)
        {
            throw new ConflictException("A restaurant cannot refer itself.");
        }

        var settings = await settingsRepo.GetOrCreateAsync(request.RestaurantId, ct);
        settings.OverbookingTolerancePercent = request.OverbookingTolerancePercent;
        settings.IsFoundingRestaurant = request.IsFoundingRestaurant;
        settings.FeaturedScore = request.FeaturedScore;
        settings.ReferredByRestaurantId = request.ReferredByRestaurantId;

        await unitOfWork.SaveChangesAsync(ct);
    }
}
