using MeetingRoomReservation.Application.Interfaces;
using MeetingRoomReservation.Domain.Models.Bookings;

namespace MeetingRoomReservation.Tests.TestDoubles;

internal sealed class NoOpBookingNotifier : IBookingNotifier
{
    public Task NotifyBookingCreatedAsync(Guid resourceId, TimeSlot timeSlot, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task NotifyBookingCancelledAsync(Guid resourceId, TimeSlot timeSlot, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
