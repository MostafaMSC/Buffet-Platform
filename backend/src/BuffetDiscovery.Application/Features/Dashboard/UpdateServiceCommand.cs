using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Dashboard;

public record UpdateServiceCommand(int Id, ServiceInput Service) : IRequest;

public class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceCommandValidator()
    {
        RuleFor(x => x.Service).NotNull().SetValidator(new ServiceInputValidator());
    }
}

public class UpdateServiceCommandHandler(
    IServiceRepository services,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateServiceCommand>
{
    public async Task Handle(UpdateServiceCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var service = await services.GetByIdForRestaurantAsync(request.Id, restaurantId, ct)
            ?? throw new NotFoundException("Service not found.");

        ServiceWriter.Apply(service, request.Service);
        ServiceWriter.ApplySlots(service, request.Service.Slots);
        ServiceWriter.ApplyPhotos(service, request.Service.PhotoUrls);
        ServiceWriter.ApplyMenu(service, request.Service.Menu);

        await unitOfWork.SaveChangesAsync(ct);
    }
}
