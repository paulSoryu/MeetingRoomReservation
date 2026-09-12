using MeetingRoomReservation.Domain.Results;

namespace MeetingRoomReservation.Domain.Models.Bookings;

public static class BookingErrors
{
    public static ValidationError InvalidTimeSlot { get; } =
        new("Booking.InvalidTimeSlot", "Slot end time must be after the slot start time.");

    public static ValidationError ResourceRequired { get; } =
        new("Booking.ResourceRequired", "A resource is required.");

    public static ValidationError UserRequired { get; } =
        new("Booking.UserRequired", "A requesting user is required.");

    public static ConflictError SlotAlreadyBooked { get; } =
        new("Booking.SlotAlreadyBooked", "The requested time slot is already booked.");

    public static NotFoundError NotFound(Guid bookingId) =>
        new("Booking.NotFound", $"Booking '{bookingId}' was not found.");
}
