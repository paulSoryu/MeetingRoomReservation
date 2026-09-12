using MeetingRoomReservation.Domain.Models.Bookings;
using MeetingRoomReservation.Domain.Results;

namespace MeetingRoomReservation.Domain.Models.Resources;

public sealed class Resource
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public WorkingHours WorkingHours { get; private set; }
    public TimeSpan SlotDuration { get; private set; }

    private Resource(Guid id, string name, WorkingHours workingHours, TimeSpan slotDuration)
    {
        Id = id;
        Name = name;
        WorkingHours = workingHours;
        SlotDuration = slotDuration;
    }

    public static Result<Resource> Create(string name, WorkingHours workingHours, TimeSpan slotDuration)
    {
        var validation = Validate(name, slotDuration);
        if (validation.IsFailure)
            return Result.Failure<Resource>(validation.Error!);

        return Result.Success(new Resource(Guid.NewGuid(), name.Trim(), workingHours, slotDuration));
    }

    public Result Update(string name, WorkingHours workingHours, TimeSpan slotDuration)
    {
        var validation = Validate(name, slotDuration);
        if (validation.IsFailure)
            return validation;

        Name = name.Trim();
        WorkingHours = workingHours;
        SlotDuration = slotDuration;
        return Result.Success();
    }

    public IReadOnlyList<TimeSlot> GenerateSlots(DateOnly date)
    {
        var slots = new List<TimeSlot>();
        var slotStart = date.ToDateTime(WorkingHours.StartTime);
        var dayEnd = date.ToDateTime(WorkingHours.EndTime);

        while (slotStart + SlotDuration <= dayEnd)
        {
            var slotEnd = slotStart + SlotDuration;
            slots.Add(TimeSlot.Create(slotStart, slotEnd).Value);
            slotStart = slotEnd;
        }

        return slots;
    }

    private static Result Validate(string name, TimeSpan slotDuration)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(ResourceErrors.NameRequired);

        if (slotDuration <= TimeSpan.Zero)
            return Result.Failure(ResourceErrors.InvalidSlotDuration);

        return Result.Success();
    }
}
