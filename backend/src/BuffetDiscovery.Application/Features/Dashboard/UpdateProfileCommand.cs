using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Dashboard;

public record UpdateProfileCommand(
    string Name,
    string NameAr,
    int AreaId,
    string PhoneNumber,
    string? Address,
    string? GoogleMapsUrl,
    string? Description,
    string? DescriptionAr,
    string? LogoUrl,
    string? CoverPhotoUrl
) : IRequest;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty();
        RuleFor(x => x.AreaId).GreaterThan(0);
        RuleFor(x => x.PhoneNumber).NotEmpty();
    }
}

public class UpdateProfileCommandHandler(
    IRestaurantRepository restaurants,
    IAreaRepository areas,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateProfileCommand>
{
    public async Task Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var r = await restaurants.GetByIdAsync(restaurantId, ct) ?? throw new NotFoundException("Restaurant not found.");

        if (!await areas.ExistsAsync(request.AreaId, ct))
        {
            throw new Common.Exceptions.ValidationException([
                new FluentValidation.Results.ValidationFailure(nameof(request.AreaId), "Invalid area.")
            ]);
        }

        r.Name = request.Name;
        r.NameAr = request.NameAr;
        r.AreaId = request.AreaId;
        r.PhoneNumber = request.PhoneNumber;
        r.Address = request.Address;
        r.GoogleMapsUrl = request.GoogleMapsUrl;
        r.Description = request.Description;
        r.DescriptionAr = request.DescriptionAr;
        r.LogoUrl = request.LogoUrl;
        r.CoverPhotoUrl = request.CoverPhotoUrl;

        await unitOfWork.SaveChangesAsync(ct);
    }
}
