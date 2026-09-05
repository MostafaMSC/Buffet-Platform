using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using BuffetDiscovery.Domain.Services;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Dashboard;

public record CreateServiceCommand(ServiceInput Service) : IRequest<int>;

public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(x => x.Service).NotNull().SetValidator(new ServiceInputValidator());
    }
}

public class CreateServiceCommandHandler(
    IServiceRepository services,
    IAvailabilityRepository availability,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateServiceCommand, int>
{
    public async Task<int> Handle(CreateServiceCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");

        var service = new Service { RestaurantId = restaurantId };
        ServiceWriter.Apply(service, request.Service);
        ServiceWriter.ApplySlots(service, request.Service.Slots);
        ServiceWriter.ApplyPhotos(service, request.Service.PhotoUrls);
        ServiceWriter.ApplyMenu(service, request.Service.Menu);

        services.Add(service);
        await unitOfWork.SaveChangesAsync(ct);

        // Materialize the next fortnight so the restaurant's calendar and the day toggles
        // have rows to switch, rather than appearing empty until someone browses.
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
        for (var date = today; date <= today.AddDays(13); date = date.AddDays(1))
        {
            availability.Add(new AvailabilityStatus
            {
                ServiceId = service.Id,
                Date = date,
                IsActive = RecurrenceEvaluator.MatchesRecurrence(service, date)
            });
        }
        await unitOfWork.SaveChangesAsync(ct);

        return service.Id;
    }
}
