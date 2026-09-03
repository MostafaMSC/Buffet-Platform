using BuffetDiscovery.Application.Common;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Domain.Entities;
using FluentValidation;

namespace BuffetDiscovery.Application.Features.Dashboard;

public record MenuItemInput(string Name, string NameAr, string? Description, string? DescriptionAr, string[]? Dietary);

public record MenuSectionInput(string Name, string NameAr, List<MenuItemInput>? Items);

public record TimeSlotInput(string StartTime, string EndTime, int Capacity, int BufferMinutes = 0);

/// The full editable shape of a service. Create and update take the same payload so the
/// restaurant's editor has one form, not two that can drift apart.
public record ServiceInput(
    ServiceType ServiceType,
    string Name,
    string NameAr,
    string? Description,
    string? DescriptionAr,
    MealType MealType,
    string[]? Cuisines,
    string[]? Dietary,
    ServiceStatus Status,

    PricingModel PricingModel,
    decimal PricePerAdult,
    decimal? PricePerChild,
    int? ChildAgeFrom,
    int? ChildAgeTo,
    int? FreeUnderAge,
    decimal? PackagePrice,
    int? PackageGuests,

    int MinGuests,
    int? MaxGuests,
    int? DurationMinutes,

    string OpensAt,
    string ClosesAt,
    RecurrenceType Recurrence,
    List<string>? Weekdays,
    DateOnly? RamadanStartDate,
    DateOnly? RamadanEndDate,
    DateOnly? OneOffDate,

    BookingMode BookingMode,
    int MinAdvanceMinutes,
    int? CancellationCutoffMinutes,

    /// Whole-window capacity. Ignored when Slots are supplied — a service is divided into
    /// slots or sold as one window, never both.
    int? Capacity,
    List<TimeSlotInput>? Slots,

    List<string>? PhotoUrls,
    string? VideoUrl,
    List<MenuSectionInput>? Menu
);

public class ServiceInputValidator : AbstractValidator<ServiceInput>
{
    public ServiceInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.DescriptionAr).MaximumLength(2000);

        RuleFor(x => x.OpensAt).Must(BeTime).WithMessage("Invalid time format, expected HH:mm.");
        RuleFor(x => x.ClosesAt).Must(BeTime).WithMessage("Invalid time format, expected HH:mm.");
        RuleFor(x => x).Must(x => !BeTime(x.OpensAt) || !BeTime(x.ClosesAt) || TimeOnly.Parse(x.OpensAt) < TimeOnly.Parse(x.ClosesAt))
            .WithMessage("Opening time must be before closing time.");

        RuleFor(x => x.MinGuests).GreaterThan(0);
        RuleFor(x => x.MaxGuests).GreaterThanOrEqualTo(x => x.MinGuests)
            .When(x => x.MaxGuests.HasValue)
            .WithMessage("Maximum guests cannot be below minimum guests.");
        RuleFor(x => x.DurationMinutes).GreaterThan(0).When(x => x.DurationMinutes.HasValue);
        RuleFor(x => x.MinAdvanceMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CancellationCutoffMinutes).GreaterThanOrEqualTo(0).When(x => x.CancellationCutoffMinutes.HasValue);
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);

        // A service that is live but has neither sittings nor a whole-window capacity
        // can't actually be booked — it would only surface as a confusing error the moment
        // a guest tried. Catch that at save time rather than at booking time.
        When(x => x.Status == ServiceStatus.Active, () =>
            RuleFor(x => x).Must(x => x.Capacity.HasValue || (x.Slots?.Count ?? 0) > 0)
                .WithMessage("An active service needs either sittings or a capacity before guests can book it."));

        When(x => x.PricingModel == PricingModel.PerPerson, () =>
        {
            RuleFor(x => x.PricePerAdult).GreaterThan(0)
                .WithMessage("A per-person service needs an adult price.");
            RuleFor(x => x.PricePerChild).GreaterThanOrEqualTo(0).When(x => x.PricePerChild.HasValue);
        });

        When(x => x.PricingModel == PricingModel.PerPackage, () =>
        {
            RuleFor(x => x.PackagePrice).NotNull().GreaterThan(0)
                .WithMessage("A package service needs a package price.");
            RuleFor(x => x.PackageGuests).NotNull().GreaterThan(0)
                .WithMessage("Say how many guests the package covers.");
        });

        RuleFor(x => x.VideoUrl)
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Video link must be a valid URL.");

        When(x => x.Recurrence == RecurrenceType.SpecificWeekdays, () =>
            RuleFor(x => x.Weekdays).NotEmpty().WithMessage("Select at least one weekday."));

        When(x => x.Recurrence == RecurrenceType.RamadanMode, () =>
        {
            RuleFor(x => x.RamadanStartDate).NotNull();
            RuleFor(x => x.RamadanEndDate).NotNull();
            RuleFor(x => x).Must(x => !x.RamadanStartDate.HasValue || !x.RamadanEndDate.HasValue || x.RamadanStartDate <= x.RamadanEndDate)
                .WithMessage("Ramadan start date must be on or before the end date.");
        });

        When(x => x.Recurrence == RecurrenceType.OneOff, () =>
            RuleFor(x => x.OneOffDate).NotNull());

        RuleForEach(x => x.Slots).ChildRules(slot =>
        {
            slot.RuleFor(s => s.StartTime).Must(BeTime).WithMessage("Invalid slot time.");
            slot.RuleFor(s => s.EndTime).Must(BeTime).WithMessage("Invalid slot time.");
            slot.RuleFor(s => s.Capacity).GreaterThan(0);
            slot.RuleFor(s => s.BufferMinutes).GreaterThanOrEqualTo(0);
            slot.RuleFor(s => s).Must(s => !BeTime(s.StartTime) || !BeTime(s.EndTime) || TimeOnly.Parse(s.StartTime) < TimeOnly.Parse(s.EndTime))
                .WithMessage("Slot start must be before slot end.");
        });
    }

    private static bool BeTime(string value) => TimeOnly.TryParse(value, out _);
}

/// Shared write logic so create and update apply the payload identically.
public static class ServiceWriter
{
    public static void Apply(Service service, ServiceInput input)
    {
        service.ServiceType = input.ServiceType;
        service.Name = input.Name.Trim();
        service.NameAr = input.NameAr.Trim();
        service.Description = input.Description?.Trim();
        service.DescriptionAr = input.DescriptionAr?.Trim();
        service.MealType = input.MealType;
        service.Cuisines = FlagEnums.Combine<Cuisines>(input.Cuisines);
        service.Dietary = FlagEnums.Combine<DietaryTags>(input.Dietary);
        service.Status = input.Status;

        service.PricingModel = input.PricingModel;
        service.PricePerAdult = input.PricingModel == PricingModel.PerPackage ? 0 : input.PricePerAdult;
        service.PricePerChild = input.PricingModel == PricingModel.PerPackage ? null : input.PricePerChild;
        service.ChildAgeFrom = input.ChildAgeFrom;
        service.ChildAgeTo = input.ChildAgeTo;
        service.FreeUnderAge = input.FreeUnderAge;
        service.PackagePrice = input.PricingModel == PricingModel.PerPackage ? input.PackagePrice : null;
        service.PackageGuests = input.PricingModel == PricingModel.PerPackage ? input.PackageGuests : null;

        service.MinGuests = input.MinGuests;
        service.MaxGuests = input.MaxGuests;
        service.DurationMinutes = input.DurationMinutes;

        service.OpensAt = TimeOnly.Parse(input.OpensAt);
        service.ClosesAt = TimeOnly.Parse(input.ClosesAt);
        service.Recurrence = input.Recurrence;
        service.Weekdays = WeekdayMapper.ToFlags(input.Weekdays);
        service.RamadanStartDate = input.Recurrence == RecurrenceType.RamadanMode ? input.RamadanStartDate : null;
        service.RamadanEndDate = input.Recurrence == RecurrenceType.RamadanMode ? input.RamadanEndDate : null;
        service.OneOffDate = input.Recurrence == RecurrenceType.OneOff ? input.OneOffDate : null;

        service.BookingMode = input.BookingMode;
        service.MinAdvanceMinutes = input.MinAdvanceMinutes;
        service.CancellationCutoffMinutes = input.CancellationCutoffMinutes;
        service.VideoUrl = string.IsNullOrWhiteSpace(input.VideoUrl) ? null : input.VideoUrl.Trim();

        // Slot-divided and whole-window capacity are mutually exclusive: keeping both would
        // leave two different answers to "how many seats are left".
        service.Capacity = input.Slots is { Count: > 0 } ? null : input.Capacity;
    }

    public static void ApplySlots(Service service, List<TimeSlotInput>? slots)
    {
        if (slots is null) return;

        var incoming = slots
            .Select(s => (Start: TimeOnly.Parse(s.StartTime), End: TimeOnly.Parse(s.EndTime), s.Capacity, s.BufferMinutes))
            .OrderBy(s => s.Start)
            .ToList();

        for (var i = 1; i < incoming.Count; i++)
        {
            if (incoming[i].Start < incoming[i - 1].End)
            {
                throw new ConflictException("Time slots cannot overlap.");
            }
        }

        var existing = service.TimeSlots.Where(s => !s.IsDeleted).OrderBy(s => s.StartTime).ToList();

        // Update slots in place where possible: existing bookings point at slot ids, so
        // replacing the rows outright would orphan them.
        for (var i = 0; i < incoming.Count; i++)
        {
            if (i < existing.Count)
            {
                existing[i].StartTime = incoming[i].Start;
                existing[i].EndTime = incoming[i].End;
                existing[i].Capacity = incoming[i].Capacity;
                existing[i].BufferMinutes = incoming[i].BufferMinutes;
            }
            else
            {
                service.TimeSlots.Add(new TimeSlot
                {
                    StartTime = incoming[i].Start,
                    EndTime = incoming[i].End,
                    Capacity = incoming[i].Capacity,
                    BufferMinutes = incoming[i].BufferMinutes
                });
            }
        }

        // Slots the restaurant removed are soft-deleted so past bookings keep their times.
        for (var i = incoming.Count; i < existing.Count; i++)
        {
            existing[i].IsDeleted = true;
        }
    }

    public static void ApplyPhotos(Service service, List<string>? photoUrls)
    {
        if (photoUrls is null) return;

        service.Photos.Clear();
        for (var i = 0; i < photoUrls.Count; i++)
        {
            service.Photos.Add(new ServicePhoto { Url = photoUrls[i], SortOrder = i });
        }
    }

    public static void ApplyMenu(Service service, List<MenuSectionInput>? menu)
    {
        if (menu is null) return;

        // Menus are replaced wholesale — nothing references a menu row, so rebuilding is
        // simpler and matches how the editor sends the whole menu back each save.
        service.MenuSections.Clear();

        var sectionOrder = 0;
        foreach (var section in menu.Where(s => !string.IsNullOrWhiteSpace(s.Name) || !string.IsNullOrWhiteSpace(s.NameAr)))
        {
            var entity = new MenuSection
            {
                Name = section.Name.Trim(),
                NameAr = section.NameAr.Trim(),
                SortOrder = sectionOrder++
            };

            var itemOrder = 0;
            foreach (var item in (section.Items ?? []).Where(i => !string.IsNullOrWhiteSpace(i.Name) || !string.IsNullOrWhiteSpace(i.NameAr)))
            {
                entity.Items.Add(new MenuItem
                {
                    Name = item.Name.Trim(),
                    NameAr = item.NameAr.Trim(),
                    Description = item.Description?.Trim(),
                    DescriptionAr = item.DescriptionAr?.Trim(),
                    Dietary = FlagEnums.Combine<DietaryTags>(item.Dietary),
                    SortOrder = itemOrder++
                });
            }

            service.MenuSections.Add(entity);
        }
    }
}
