using MeetingRoomReservation.Domain.Results;

namespace MeetingRoomReservation.Domain.Models.Bookings;

public sealed class Booking
{
    public Guid Id { get; private set; }
    public Guid ResourceId { get; private set; }
    public TimeSlot TimeSlot { get; private set; }
    public string UserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Booking(Guid id, Guid resourceId, TimeSlot timeSlot, string userId, DateTime createdAtUtc)
    {
        Id = id;
        ResourceId = resourceId;
        TimeSlot = timeSlot;
        UserId = userId;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<Booking> Create(Guid resourceId, TimeSlot timeSlot, string userId)
    {
        if (resourceId == Guid.Empty)
            return Result.Failure<Booking>(BookingErrors.ResourceRequired);

        if (string.IsNullOrWhiteSpace(userId))
            return Result.Failure<Booking>(BookingErrors.UserRequired);

        return Result.Success(new Booking(Guid.NewGuid(), resourceId, timeSlot, userId, DateTime.UtcNow));
    }
}
