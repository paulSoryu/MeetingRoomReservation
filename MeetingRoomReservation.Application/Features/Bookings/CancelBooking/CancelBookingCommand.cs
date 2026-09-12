using FluentValidation;
using MediatR;
using MeetingRoomReservation.Application.Interfaces;
using MeetingRoomReservation.Domain.Models.Bookings;
using MeetingRoomReservation.Domain.Results;
using Microsoft.Extensions.Logging;

namespace MeetingRoomReservation.Application.Features.Bookings.CancelBooking;

public sealed record CancelBookingCommand(Guid BookingId, string RequestingUserId, bool IsAdmin) : IRequest<Result>;

public sealed class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingCommandValidator()
    {
        RuleFor(c => c.BookingId).NotEmpty();
        RuleFor(c => c.RequestingUserId).NotEmpty();
    }
}

public sealed class CancelBookingCommandHandler(
    IBookingRepository bookingRepository,
    IBookingNotifier bookingNotifier,
    ILogger<CancelBookingCommandHandler> logger)
    : IRequestHandler<CancelBookingCommand, Result>
{
    public async Task<Result> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

        // A non-owner, non-admin request is reported as NotFound rather than leaking that the booking exists.
        if (booking is null || (!request.IsAdmin && booking.UserId != request.RequestingUserId))
            return Result.Failure(BookingErrors.NotFound(request.BookingId));

        await bookingRepository.DeleteAsync(booking, cancellationToken);

        // Best-effort: the cancellation is already committed, so a notification outage must not
        // turn a successful cancellation into a failed request.
        try
        {
            await bookingNotifier.NotifyBookingCancelledAsync(booking.ResourceId, booking.TimeSlot, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to send booking-cancelled notification for booking {BookingId}", booking.Id);
        }

        return Result.Success();
    }
}
