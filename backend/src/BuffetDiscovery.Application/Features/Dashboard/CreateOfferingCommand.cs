using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using BuffetDiscovery.Domain.Services;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Dashboard;

public record CreateOfferingCommand(
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
    List<string>? PhotoUrls,
    string? VideoUrl
) : IRequest<int>;

public class CreateOfferingCommandValidator : AbstractValidator<CreateOfferingCommand>
{
    public CreateOfferingCommandValidator()
    {
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OpensAt).Must(t => TimeOnly.TryParse(t, out _)).WithMessage("Invalid time format, expected HH:mm.");
        RuleFor(x => x.ClosesAt).Must(t => TimeOnly.TryParse(t, out _)).WithMessage("Invalid time format, expected HH:mm.");
        RuleFor(x => x.VideoUrl)
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Video link must be a valid URL.");

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

public class CreateOfferingCommandHandler(
    IOfferingRepository offerings,
    IAvailabilityRepository availability,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateOfferingCommand, int>
{
    public async Task<int> Handle(CreateOfferingCommand request, CancellationToken ct)
    {
        var restaurantId = currentUser.RestaurantId ?? throw new UnauthorizedException("No restaurant associated with this account.");

        var offering = new BuffetOffering
        {
            RestaurantId = restaurantId,
            MealType = request.MealType,
            Price = request.Price,
            OpensAt = TimeOnly.Parse(request.OpensAt),
            ClosesAt = TimeOnly.Parse(request.ClosesAt),
            Description = request.Description,
            DescriptionAr = request.DescriptionAr,
            Recurrence = request.Recurrence,
            Weekdays = WeekdayMapper.ToFlags(request.Weekdays),
            RamadanStartDate = request.RamadanStartDate,
            RamadanEndDate = request.RamadanEndDate,
            OneOffDate = request.OneOffDate,
            VideoUrl = request.VideoUrl
        };

        if (request.PhotoUrls is not null)
        {
            offering.Photos = request.PhotoUrls
                .Select((url, i) => new OfferingPhoto { Url = url, SortOrder = i })
                .ToList();
        }

        offerings.Add(offering);
        await unitOfWork.SaveChangesAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
        for (var date = today; date <= today.AddDays(13); date = date.AddDays(1))
        {
            availability.Add(new AvailabilityStatus
            {
                OfferingId = offering.Id,
                Date = date,
                IsActive = RecurrenceEvaluator.MatchesRecurrence(offering, date)
            });
        }
        await unitOfWork.SaveChangesAsync(ct);

        return offering.Id;
    }
}
