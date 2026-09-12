using MeetingRoomReservation.Domain.Results;

namespace MeetingRoomReservation.Domain.Models.Bookings;

public sealed record TimeSlot
{
    public DateTime SlotStart { get; }
    public DateTime SlotEnd { get; }

    private TimeSlot(DateTime slotStart, DateTime slotEnd)
    {
        SlotStart = slotStart;
        SlotEnd = slotEnd;
    }

    public static Result<TimeSlot> Create(DateTime slotStart, DateTime slotEnd)
    {
        if (slotEnd <= slotStart)
            return Result.Failure<TimeSlot>(BookingErrors.InvalidTimeSlot);

        return Result.Success(new TimeSlot(slotStart, slotEnd));
    }
}
