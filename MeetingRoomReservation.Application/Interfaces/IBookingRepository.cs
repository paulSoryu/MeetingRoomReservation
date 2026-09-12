using MeetingRoomReservation.Domain.Models.Bookings;
using MeetingRoomReservation.Domain.Results;

namespace MeetingRoomReservation.Application.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Booking>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Booking>> ListByUserIdAsync(string userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Booking>> ListByResourceAndDateAsync(Guid resourceId, DateOnly date, CancellationToken cancellationToken);

    // Must not throw on a unique-index violation - translates it into a ConflictError instead.
    Task<Result> AddAsync(Booking booking, CancellationToken cancellationToken);

    Task DeleteAsync(Booking booking, CancellationToken cancellationToken);
}
