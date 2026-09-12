namespace MeetingRoomReservation.Api.DTOs;

public sealed record CreateResourceRequest(
    string Name,
    TimeOnly WorkingHoursStart,
    TimeOnly WorkingHoursEnd,
    TimeSpan SlotDuration);
