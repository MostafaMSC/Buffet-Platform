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
    double? Latitude,
    double? Longitude,
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

        // A pin is optional, but a half-set pair would silently drop the venue off distance
        // search, and out-of-range values would place it somewhere impossible.
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        RuleFor(x => x.Longitude).NotNull().When(x => x.Latitude.HasValue)
            .WithMessage("A map pin needs both a latitude and a longitude.");
        RuleFor(x => x.Latitude).NotNull().When(x => x.Longitude.HasValue)
            .WithMessage("A map pin needs both a latitude and a longitude.");
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
        r.Latitude = request.Latitude;
        r.Longitude = request.Longitude;
        r.Description = request.Description;
        r.DescriptionAr = request.DescriptionAr;
        r.LogoUrl = request.LogoUrl;
        r.CoverPhotoUrl = request.CoverPhotoUrl;

        await unitOfWork.SaveChangesAsync(ct);
    }
}
