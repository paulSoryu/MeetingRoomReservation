using MeetingRoomReservation.Domain.Models.Bookings;

namespace MeetingRoomReservation.Application.Interfaces;

public interface IBookingNotifier
{
    Task NotifyBookingCreatedAsync(Guid resourceId, TimeSlot timeSlot, CancellationToken cancellationToken);
    Task NotifyBookingCancelledAsync(Guid resourceId, TimeSlot timeSlot, CancellationToken cancellationToken);
}
