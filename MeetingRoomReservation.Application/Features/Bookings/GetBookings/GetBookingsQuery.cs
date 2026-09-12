using MediatR;
using MeetingRoomReservation.Application.Features.Bookings;
using MeetingRoomReservation.Application.Interfaces;
using MeetingRoomReservation.Domain.Results;

namespace MeetingRoomReservation.Application.Features.Bookings.GetBookings;

// UserId is null for the admin "all bookings" view, or set to scope results to a single user's own bookings.
public sealed record GetBookingsQuery(string? UserId) : IRequest<Result<IReadOnlyList<BookingResponse>>>;

public sealed class GetBookingsQueryHandler(IBookingRepository bookingRepository)
    : IRequestHandler<GetBookingsQuery, Result<IReadOnlyList<BookingResponse>>>
{
    public async Task<Result<IReadOnlyList<BookingResponse>>> Handle(GetBookingsQuery request, CancellationToken cancellationToken)
    {
        var bookings = string.IsNullOrEmpty(request.UserId)
            ? await bookingRepository.ListAsync(cancellationToken)
            : await bookingRepository.ListByUserIdAsync(request.UserId, cancellationToken);

        var responses = bookings.Select(booking => booking.ToResponse()).ToList();

        return Result.Success<IReadOnlyList<BookingResponse>>(responses);
    }
}
