using MeetingRoomReservation.Domain.Models.Resources;

namespace MeetingRoomReservation.Application.Features.Resources;

public sealed record ResourceResponse(
    Guid Id,
    string Name,
    TimeOnly WorkingHoursStart,
    TimeOnly WorkingHoursEnd,
    TimeSpan SlotDuration);

public sealed record ScheduleResponse(
    Guid ResourceId,
    DateOnly Date,
    IReadOnlyList<SlotResponse> Slots);

public sealed record SlotResponse(
    DateTime SlotStart,
    DateTime SlotEnd,
    bool IsBooked,
    Guid? BookingId);

internal static class ResourceMapping
{
    public static ResourceResponse ToResponse(this Resource resource) => new(
        resource.Id,
        resource.Name,
        resource.WorkingHours.StartTime,
        resource.WorkingHours.EndTime,
        resource.SlotDuration);
}
