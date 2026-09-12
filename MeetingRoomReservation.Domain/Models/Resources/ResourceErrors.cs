using MeetingRoomReservation.Domain.Results;

namespace MeetingRoomReservation.Domain.Models.Resources;

public static class ResourceErrors
{
    public static ValidationError NameRequired { get; } =
        new("Resource.NameRequired", "Resource name is required.");

    public static ValidationError InvalidWorkingHours { get; } =
        new("Resource.InvalidWorkingHours", "Working hours start time must be before the end time.");

    public static ValidationError InvalidSlotDuration { get; } =
        new("Resource.InvalidSlotDuration", "Slot duration must be a positive value.");

    public static NotFoundError NotFound(Guid resourceId) =>
        new("Resource.NotFound", $"Resource '{resourceId}' was not found.");
}
