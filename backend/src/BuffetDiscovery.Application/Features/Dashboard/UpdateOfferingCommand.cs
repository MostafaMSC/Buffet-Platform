using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Dashboard;

public record UpdateOfferingCommand(
    int Id,
    MealType MealType,
    decimal Price,
    string OpensAt,
    string ClosesAt,
    string? Description,
    string? DescriptionAr,
    RecurrenceType Recurrence,
    List<string>? Weekdays,
    DateOnly? RamadanStartDate,
    DateOnly? RamadanEndDate,
    DateOnly? OneOffDate,
    List<string>? PhotoUrls
) : IRequest;

public class UpdateOfferingCommandValidator : AbstractValidator<UpdateOfferingCommand>
{
    public UpdateOfferingCommandValidator()
    {
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OpensAt).Must(t => TimeOnly.TryParse(t, out _)).WithMessage("Invalid time format, expected HH:mm.");
        RuleFor(x => x.ClosesAt).Must(t => TimeOnly.TryParse(t, out _)).WithMessage("Invalid time format, expected HH:mm.");

        When(x => x.Recurrence == RecurrenceType.SpecificWeekdays, () =>
        {
            RuleFor(x => x.Weekdays).NotEmpty().WithMessage("Select at least one weekday.");
        });

        When(x => x.Recurrence == RecurrenceType.RamadanMode, () =>
        {
            RuleFor(x => x.RamadanStartDate).NotNull();
            RuleFor(x => x.RamadanEndDate).NotNull();
            RuleFor(x => x).Must(x => !x.RamadanStartDate.HasValue || !x.RamadanEndDate.HasValue || x.RamadanStartDate <= x.RamadanEndDate)
                .WithMessage("Ramadan start date must be on or before the end date.");
        });

        When(x => x.Recurrence == RecurrenceType.OneOff, () =>
        {
            RuleFor(x => x.OneOffDate).NotNull();
        });
    }
}

public class UpdateOfferingCommandHandler(
    IOfferingRepository offerings,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateOfferingCommand>
{
    public async Task Handle(UpdateOfferingCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");
        var offering = await offerings.GetByIdForRestaurantAsync(request.Id, restaurantId, ct)
            ?? throw new NotFoundException("Offering not found.");

        offering.MealType = request.MealType;
        offering.Price = request.Price;
        offering.OpensAt = TimeOnly.Parse(request.OpensAt);
        offering.ClosesAt = TimeOnly.Parse(request.ClosesAt);
        offering.Description = request.Description;
        offering.DescriptionAr = request.DescriptionAr;
        offering.Recurrence = request.Recurrence;
        offering.Weekdays = WeekdayMapper.ToFlags(request.Weekdays);
        offering.RamadanStartDate = request.RamadanStartDate;
        offering.RamadanEndDate = request.RamadanEndDate;
        offering.OneOffDate = request.OneOffDate;

        if (request.PhotoUrls is not null)
        {
            offerings.RemovePhotos(offering.Photos);
            offering.Photos = request.PhotoUrls
                .Select((url, i) => new OfferingPhoto { Url = url, SortOrder = i })
                .ToList();
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
