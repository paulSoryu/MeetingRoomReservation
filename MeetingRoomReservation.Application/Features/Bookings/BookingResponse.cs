using MeetingRoomReservation.Domain.Models.Bookings;

namespace MeetingRoomReservation.Application.Features.Bookings;

public sealed record BookingResponse(
    Guid Id,
    Guid ResourceId,
    DateTime SlotStart,
    DateTime SlotEnd,
    string UserId,
    DateTime CreatedAtUtc);

internal static class BookingMapping
{
    public static BookingResponse ToResponse(this Booking booking) => new(
        booking.Id,
        booking.ResourceId,
        booking.TimeSlot.SlotStart,
        booking.TimeSlot.SlotEnd,
        booking.UserId,
        booking.CreatedAtUtc);
}
