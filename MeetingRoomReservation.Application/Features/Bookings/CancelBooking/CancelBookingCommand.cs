using FluentValidation;
using MediatR;
using MeetingRoomReservation.Application.Interfaces;
using MeetingRoomReservation.Domain.Models.Bookings;
using MeetingRoomReservation.Domain.Results;

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
    IBookingNotifier bookingNotifier)
    : IRequestHandler<CancelBookingCommand, Result>
{
    public async Task<Result> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);

        // A non-owner, non-admin request is reported as NotFound rather than leaking that the booking exists.
        if (booking is null || (!request.IsAdmin && booking.UserId != request.RequestingUserId))
            return Result.Failure(BookingErrors.NotFound(request.BookingId));

        await bookingRepository.DeleteAsync(booking, cancellationToken);
        await bookingNotifier.NotifyBookingCancelledAsync(booking.ResourceId, booking.TimeSlot, cancellationToken);

        return Result.Success();
    }
}
