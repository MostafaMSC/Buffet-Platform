using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Auth;

public record SignupCommand(
    string PhoneNumber,
    string Password,
    string RestaurantName,
    string RestaurantNameAr,
    int AreaId
) : IRequest<AuthResponseDto>;

public class SignupCommandValidator : AbstractValidator<SignupCommand>
{
    public SignupCommandValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.RestaurantName).NotEmpty();
        RuleFor(x => x.RestaurantNameAr).NotEmpty();
        RuleFor(x => x.AreaId).GreaterThan(0);
    }
}

public class SignupCommandHandler(
    IUserRepository users,
    IRestaurantRepository restaurants,
    IAreaRepository areas,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IUnitOfWork unitOfWork) : IRequestHandler<SignupCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(SignupCommand request, CancellationToken ct)
    {
        if (await users.PhoneNumberExistsAsync(request.PhoneNumber, ct))
        {
            throw new ConflictException("This phone number is already registered.");
        }

        if (!await areas.ExistsAsync(request.AreaId, ct))
        {
            throw new Common.Exceptions.ValidationException([
                new FluentValidation.Results.ValidationFailure(nameof(request.AreaId), "Invalid area.")
            ]);
        }

        var restaurant = new Restaurant
        {
            Name = request.RestaurantName,
            NameAr = request.RestaurantNameAr,
            AreaId = request.AreaId,
            PhoneNumber = request.PhoneNumber,
            Status = RestaurantStatus.Pending
        };
        restaurants.Add(restaurant);
        await unitOfWork.SaveChangesAsync(ct);

        var user = new User
        {
            PhoneNumber = request.PhoneNumber,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = UserRole.RestaurantOwner,
            RestaurantId = restaurant.Id
        };
        users.Add(user);
        await unitOfWork.SaveChangesAsync(ct);

        var token = jwtTokenService.CreateToken(user);
        return new AuthResponseDto(token, user.Role.ToString(), restaurant.Id, restaurant.Status);
    }
}
