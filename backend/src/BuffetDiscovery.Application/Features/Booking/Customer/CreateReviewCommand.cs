using BuffetDiscovery.Application.Common.Dtos;
using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using FluentValidation;
using MediatR;

namespace BuffetDiscovery.Application.Features.Booking.Customer;

/// A guest reviews their own visit by the same confirmation code that identifies the
/// booking everywhere else — no account, and the review is automatically "verified"
/// because it's tied to a real, completed booking.
public record CreateReviewCommand(string ConfirmationCode, int Rating, string? Comment) : IRequest<ReviewDto>;

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.ConfirmationCode).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}

public class CreateReviewCommandHandler(
    IBookingRepository bookingRepo,
    IServiceRepository serviceRepo,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(CreateReviewCommand request, CancellationToken ct)
    {
        var booking = await bookingRepo.GetByConfirmationCodeAsync(request.ConfirmationCode.Trim(), ct)
            ?? throw new NotFoundException("Booking not found.", "booking_not_found");

        if (booking.Status != BookingStatus.Completed)
        {
            throw new ConflictException(
                "You can review this booking once your visit is marked complete.", "review_not_completed");
        }

        var reviewed = await serviceRepo.GetReviewedBookingIdsAsync([booking.Id], ct);
        if (reviewed.Contains(booking.Id))
        {
            throw new ConflictException("You've already reviewed this booking.", "review_already_submitted");
        }

        var review = new Review
        {
            RestaurantId = booking.Service!.RestaurantId,
            ServiceId = booking.ServiceId,
            BookingId = booking.Id,
            CustomerName = booking.CustomerName,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
        };

        serviceRepo.AddReview(review);
        await unitOfWork.SaveChangesAsync(ct);

        return new ReviewDto(review.Id, review.CustomerName, review.Rating, review.Comment, review.CreatedAt, true);
    }
}
